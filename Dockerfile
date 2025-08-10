# ----------- Build the application -----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first for better layer caching
COPY Empire.Shared/Empire.Shared.csproj Empire.Shared/
COPY Empire.Client/Empire.Client.csproj Empire.Client/
COPY Empire.Server/Empire.Server.csproj Empire.Server/

# Restore dependencies
RUN dotnet restore Empire.Server/Empire.Server.csproj

# Copy source code (excluding large assets via .dockerignore)
COPY . .

# Publish server, which also triggers client publish via post-build hook
RUN dotnet publish Empire.Server/Empire.Server.csproj -c Release -o /app/publish --no-restore

# ----------- Runtime image -----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "Empire.Server.dll"]
