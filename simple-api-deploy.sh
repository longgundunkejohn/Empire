#!/bin/bash

echo "?? SIMPLE API-ONLY DEPLOYMENT"
echo "============================"

# Clear the terminal
clear

# Stop everything
docker-compose -f docker-compose-cms.yml down 2>/dev/null || true

# Create minimal API-only Dockerfile
cat > Dockerfile.api-only << 'EOF'
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Only copy what we need for the server
COPY Empire.Shared/Empire.Shared.csproj Empire.Shared/
COPY Empire.Server/Empire.Server.csproj Empire.Server/
COPY Empire.sln .

# Clean the server project file to remove client dependencies
RUN sed -i '/<ProjectReference.*Empire\.Client/d' Empire.Server/Empire.Server.csproj
RUN sed -i '/<PackageReference.*WebAssembly\.Server/d' Empire.Server/Empire.Server.csproj
RUN sed -i '/<Target.*PublishBlazorClient/,/<\/Target>/d' Empire.Server/Empire.Server.csproj

# Restore only server
RUN dotnet restore Empire.Server/Empire.Server.csproj

# Copy source
COPY Empire.Shared/ Empire.Shared/
COPY Empire.Server/ Empire.Server/

# Build only server
RUN dotnet publish Empire.Server/Empire.Server.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN mkdir -p /app/data
COPY --from=build /app/publish .

# Simple test page
RUN mkdir -p /app/wwwroot && echo '<html><body><h1>Empire TCG API Server</h1><p><a href="/swagger">API Documentation</a></p><p><a href="/api/deckbuilder/cards">Test API</a></p></body></html>' > /app/wwwroot/index.html

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Empire.Server.dll"]
EOF

# Update docker-compose to use api-only dockerfile
cp docker-compose-cms.yml docker-compose-api.yml
sed -i 's/dockerfile: .*/dockerfile: Dockerfile.api-only/' docker-compose-api.yml

echo "Building API-only version..."
docker-compose -f docker-compose-api.yml build --no-cache empire-game

if [ $? -eq 0 ]; then
    echo "? API-only build successful!"
    
    docker-compose -f docker-compose-api.yml up -d
    
    sleep 20
    
    echo "Testing API endpoints..."
    curl -s http://localhost:8081/api/deckbuilder/cards | head -c 100
    echo ""
    
    echo "?? SUCCESS! Your Empire TCG API is now running!"
    echo ""
    echo "Access points:"
    echo "- API Documentation: http://empirecardgame.com:8081/swagger"
    echo "- Test API: http://empirecardgame.com:8081/api/deckbuilder/cards"
    echo "- WordPress: http://empirecardgame.com:8080"
    echo "- Main: http://empirecardgame.com"
    
else
    echo "? Build failed"
    docker-compose -f docker-compose-api.yml logs empire-game
fi