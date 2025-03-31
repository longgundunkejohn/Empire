# ----------- Build the client -----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy full repo context into Docker
COPY . .

# Restore dependencies
RUN dotnet restore Empire.Server/Empire.Server.csproj

# Publish server, which also triggers client publish via post-build hook
RUN dotnet publish Empire.Server/Empire.Server.csproj -c Release -o /app/publish

# ----------- Runtime image -----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "Empire.Server.dll"]

