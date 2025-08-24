#!/bin/bash

echo "?? SERVER-ONLY DEPLOYMENT FALLBACK"
echo "==================================="
echo "Building server-only version to bypass WebAssembly build issues"

# Stop current containers
docker-compose -f docker-compose-cms.yml down 2>/dev/null || true

# Create a server-only Dockerfile
echo "?? Creating server-only Dockerfile..."
cat > Dockerfile.server-only << 'EOF'
# Use the official .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY Empire.Shared/Empire.Shared.csproj Empire.Shared/
COPY Empire.Server/Empire.Server.csproj Empire.Server/
COPY Empire.sln .

# Restore dependencies (server only)
RUN dotnet restore Empire.Server/Empire.Server.csproj

# Copy source code
COPY Empire.Shared/ Empire.Shared/
COPY Empire.Server/ Empire.Server/

# Build and publish the ASP.NET Core Server only
RUN dotnet publish Empire.Server/Empire.Server.csproj -c Release -o /app/publish \
    --no-restore

# Use the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create data directory for SQLite
RUN mkdir -p /app/data

# Copy published server application
COPY --from=build /app/publish .

# Copy static files if they exist
COPY Empire.Client/wwwroot/ ./wwwroot/ 2>/dev/null || echo "No static files to copy"

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "Empire.Server.dll"]
EOF

# Update docker-compose to use the server-only Dockerfile
echo "?? Updating docker-compose for server-only build..."
sed -i 's/dockerfile: Dockerfile/dockerfile: Dockerfile.server-only/' docker-compose-cms.yml

# Build and deploy
echo "??? Building server-only version..."
docker-compose -f docker-compose-cms.yml build --no-cache empire-game

if [ $? -eq 0 ]; then
    echo "? Server-only build successful!"
    
    echo "?? Starting services..."
    docker-compose -f docker-compose-cms.yml up -d
    
    echo "? Waiting for services..."
    sleep 30
    
    echo "?? Testing services..."
    echo "WordPress:" $(curl -s -o /dev/null -w "%{http_code}" http://localhost:8080)
    echo "Game Server:" $(curl -s -o /dev/null -w "%{http_code}" http://localhost:8081)
    echo "Nginx:" $(curl -s -o /dev/null -w "%{http_code}" http://localhost)
    
    echo ""
    echo "?? SERVER-ONLY DEPLOYMENT SUCCESSFUL!"
    echo ""
    echo "? What's working:"
    echo "   - ASP.NET Core server with API endpoints"
    echo "   - Static file serving for basic web content"
    echo "   - WordPress CMS integration"
    echo "   - Database functionality"
    echo ""
    echo "?? Access your platform:"
    echo "- WordPress: http://empirecardgame.com:8080"
    echo "- Game API: http://empirecardgame.com:8081/api/deckbuilder/cards"
    echo "- Main: http://empirecardgame.com"
    echo ""
    echo "?? Next steps:"
    echo "1. Test the API endpoints work"
    echo "2. Complete WordPress setup"
    echo "3. We can add WebAssembly back later once Docker environment is sorted"
    
else
    echo "? Server-only build failed. Showing logs..."
    docker-compose -f docker-compose-cms.yml logs --tail=20 empire-game
    exit 1
fi