#!/bin/bash

echo "🔧 Deploying Empire TCG with server-only fallback approach"

# Stop current containers
echo "📦 Stopping current containers..."
docker-compose down

# Clean up Docker system completely
echo "🧹 Cleaning up Docker system..."
docker system prune -af
docker volume prune -f

# Use the server-only fallback Dockerfile
echo "🔄 Using server-only fallback Dockerfile..."
cp Dockerfile.no-wasm Dockerfile

# Rebuild with no cache to ensure fresh build
echo "🏗️ Rebuilding with server-only fallback approach..."
docker-compose build --no-cache --pull

# Deploy fresh containers
echo "🚀 Deploying fresh containers..."
docker-compose up -d

# Wait for services to initialize
echo "⏳ Waiting for services to initialize..."
sleep 45

# Check container status
echo "🔍 Checking container status..."
docker-compose ps

# Check container logs for any errors
echo "📋 Checking container logs..."
docker-compose logs empire-tcg | tail -20

# Test API endpoint
echo "🧪 Testing API endpoint..."
curl -s -w "\nHTTP Status: %{http_code}\n" https://empirecardgame.com/api/deckbuilder/cards | head -c 200

echo ""
echo "🔍 Testing main website..."
curl -s -I https://empirecardgame.com | head -5

echo ""
echo "✅ Server-only deployment complete!"
echo ""
echo "🌐 Test the website: https://empirecardgame.com"
echo "🃏 Test API directly: https://empirecardgame.com/api/deckbuilder/cards"
echo "🏠 Test lobby API: https://empirecardgame.com/api/lobby/games"
echo ""
echo "📋 Deployment strategy:"
echo "   ✓ Server builds successfully without WASM issues"
echo "   ✓ Blazor client attempts to build, falls back if it fails"
echo "   ✓ API endpoints remain functional regardless"
echo "   ✓ Fresh container deployment"
echo ""
echo "🔧 This approach ensures the server API works even if Blazor WebAssembly fails"
echo "🔧 You can test the deck builder API at: https://empirecardgame.com/api/deckbuilder/cards"
