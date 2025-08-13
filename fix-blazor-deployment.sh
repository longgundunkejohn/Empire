#!/bin/bash

echo "🔧 Fixing Empire TCG Blazor Deployment Issues..."

# Stop the current deployment
echo "📦 Stopping current containers..."
docker-compose down

# Clean up any orphaned containers
echo "🧹 Cleaning up..."
docker system prune -f

# Rebuild with proper .NET framework files
echo "🏗️ Rebuilding with proper .NET framework..."
docker-compose build --no-cache

# Deploy with fresh containers
echo "🚀 Deploying fresh containers..."
docker-compose up -d

# Wait for services to start
echo "⏳ Waiting for services to initialize..."
sleep 30

# Check if services are running
echo "🔍 Checking service status..."
docker-compose ps

# Test the API endpoint
echo "🧪 Testing API endpoint..."
curl -s https://empirecardgame.com/api/deckbuilder/cards | head -c 200

# Check for the problematic framework file
echo "🔍 Checking for framework files..."
docker-compose exec web ls -la /app/wwwroot/_framework/ | grep icudt

echo "✅ Deployment fix complete!"
echo "🌐 Visit https://empirecardgame.com to test"
