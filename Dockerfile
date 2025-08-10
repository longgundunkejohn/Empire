# ----------- Build the application -----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first for better layer caching
COPY Empire.Shared/Empire.Shared.csproj Empire.Shared/
COPY Empire.Client/Empire.Client.csproj Empire.Client/
COPY Empire.Server/Empire.Server.csproj Empire.Server/

# Restore dependencies
RUN dotnet restore Empire.Server/Empire.Server.csproj

# Copy only essential source files (no images)
COPY Empire.Shared/ Empire.Shared/
COPY Empire.Client/ Empire.Client/
COPY Empire.Server/ Empire.Server/
COPY Empire.sln .
COPY nginx/ nginx/

# Remove any image directories that might have been copied
RUN rm -rf Empire.Client/wwwroot/images/Cards/ || true
RUN rm -rf Empire.Client/wwwroot/images/cards/ || true
RUN rm -rf blazor-dist/ || true

# Publish server, which also triggers client publish via post-build hook
RUN dotnet publish Empire.Server/Empire.Server.csproj -c Release -o /app/publish --no-restore

# ----------- Runtime image -----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "Empire.Server.dll"]
