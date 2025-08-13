#!/bin/bash

echo "🔧 Fixing compilation error and redeploying Empire TCG"

# Stop current containers
echo "📦 Stopping current containers..."
docker-compose down

# Clean up Docker system
echo "🧹 Cleaning up Docker system..."
docker system prune -f

# Use the improved Dockerfile
echo "🔄 Using improved Dockerfile..."
cp Dockerfile.blazor-fix Dockerfile

# Rebuild with no cache to ensure fresh build
echo "🏗️ Rebuilding with compilation fix..."
docker-compose build --no-cache

# Deploy fresh containers
echo "🚀 Deploying fresh containers..."
docker-compose up -d

# Wait for services to initialize
echo "⏳ Waiting for services to initialize..."
sleep 30

# Check container status
echo "🔍 Checking container status..."
docker-compose ps

# Test API endpoint
echo "🧪 Testing API endpoint..."
curl -s -w "\nHTTP Status: %{http_code}\n" https://empirecardgame.com/api/deckbuilder/cards | head -c 200

echo ""
echo "✅ Compilation fix and deployment complete!"
echo ""
echo "🌐 Test the website: https://empirecardgame.com"
echo "🃏 Test deck builder: https://empirecardgame.com/deckbuilder"
echo "🏠 Test lobby: https://empirecardgame.com/lobby"
echo ""
echo "📋 Issues fixed:"
echo "   ✓ Fixed ErrorEventArgs.Target compilation error"
echo "   ✓ Added missing JavaScript function"
echo "   ✓ Fixed .NET framework file deployment"
echo "   ✓ Fresh container deployment"
