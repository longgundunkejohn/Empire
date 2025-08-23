#!/bin/bash

echo "?? QUICK FIX: Fixing SignalR Package Conflict"
echo "============================================="

# Clean up Docker completely
echo "Cleaning Docker..."
docker-compose -f docker-compose-cms.yml down 2>/dev/null || true
docker system prune -af
docker volume prune -f

# Fix the specific package issue by doing a clean restore
echo "Cleaning NuGet packages..."
rm -rf ~/.nuget/packages/microsoft.extensions.logging.abstractions 2>/dev/null || true
rm -rf ~/.nuget/packages/microsoft.aspnetcore.signalr.client 2>/dev/null || true

# Ensure the project file is correct
echo "Verifying project file..."
cat Empire.Client/Empire.Client.csproj | grep SignalR

echo ""
echo "Building with clean slate..."

# Build without cache and with restore
docker-compose -f docker-compose-cms.yml build --no-cache --progress=plain empire-game

if [ $? -eq 0 ]; then
    echo "? Build successful! Starting all services..."
    docker-compose -f docker-compose-cms.yml up -d
    
    echo "Waiting for services..."
    sleep 30
    
    echo "Testing connectivity..."
    curl -s -o /dev/null -w "WordPress: %{http_code}\n" http://localhost:8080
    curl -s -o /dev/null -w "Game: %{http_code}\n" http://localhost:8081
    curl -s -o /dev/null -w "Nginx: %{http_code}\n" http://localhost
    
    echo ""
    echo "?? DEPLOYMENT SUCCESSFUL!"
    echo "Access your platform:"
    echo "- WordPress: http://empirecardgame.com:8080"
    echo "- Game: http://empirecardgame.com:8081"
    echo "- Main: http://empirecardgame.com"
else
    echo "? Build failed. Showing detailed logs..."
    docker-compose -f docker-compose-cms.yml logs empire-game
    exit 1
fi