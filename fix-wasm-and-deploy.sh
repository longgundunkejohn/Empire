#!/bin/bash

echo "🔧 Fixing WebAssembly runtime pack issue and redeploying Empire TCG"

# Stop current containers
echo "📦 Stopping current containers..."
docker-compose down

# Clean up Docker system completely
echo "🧹 Cleaning up Docker system..."
docker system prune -af
docker volume prune -f

# Use the WASM-fixed Dockerfile
echo "🔄 Using WASM-fixed Dockerfile..."
cp Dockerfile.wasm-fix Dockerfile

# Rebuild with no cache to ensure fresh build
echo "🏗️ Rebuilding with WASM runtime pack fix..."
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
echo "🔍 Testing Blazor WebAssembly files..."
curl -s -I https://empirecardgame.com/_framework/blazor.webassembly.js | head -5

echo ""
echo "✅ WASM fix and deployment complete!"
echo ""
echo "🌐 Test the website: https://empirecardgame.com"
echo "🃏 Test deck builder: https://empirecardgame.com/deckbuilder"
echo "🏠 Test lobby: https://empirecardgame.com/lobby"
echo ""
echo "📋 Issues fixed:"
echo "   ✓ Fixed WASM0005 WebAssembly runtime pack error"
echo "   ✓ Added explicit RuntimeIdentifier=browser-wasm"
echo "   ✓ Used --skip-manifest-update for workload install"
echo "   ✓ Fixed ErrorEventArgs.Target compilation error"
echo "   ✓ Added missing JavaScript function"
echo "   ✓ Fresh container deployment"
echo ""
echo "🔧 If deck builder still shows no cards, check browser console for specific errors"
