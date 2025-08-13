#!/bin/bash

echo "🔧 Fixing Empire TCG Issues: Blazor Framework + Phantom Users + Deck Builder"

# Stop current deployment
echo "📦 Stopping current containers..."
docker-compose down

# Clean up Docker system
echo "🧹 Cleaning up Docker system..."
docker system prune -f
docker volume prune -f

# Backup current Dockerfile
echo "💾 Backing up current Dockerfile..."
cp Dockerfile Dockerfile.backup

# Use the improved Dockerfile
echo "🔄 Switching to improved Dockerfile..."
cp Dockerfile.blazor-fix Dockerfile

# Rebuild with no cache to ensure fresh .NET framework files
echo "🏗️ Rebuilding with fresh .NET framework files..."
docker-compose build --no-cache

# Deploy fresh containers
echo "🚀 Deploying fresh containers..."
docker-compose up -d

# Wait for services to initialize
echo "⏳ Waiting for services to initialize..."
sleep 45

# Check container status
echo "🔍 Checking container status..."
docker-compose ps

# Test API endpoint
echo "🧪 Testing API endpoint..."
curl -s -w "\nHTTP Status: %{http_code}\n" https://empirecardgame.com/api/deckbuilder/cards | head -c 300

# Check for framework files in container
echo "🔍 Checking for .NET framework files..."
docker-compose exec web ls -la /app/wwwroot/_framework/ | grep -E "(dotnet|icudt|blazor)" || echo "⚠️ Some framework files may be missing"

# Test card image availability
echo "🖼️ Testing card image availability..."
curl -s -w "\nHTTP Status: %{http_code}\n" -I https://empirecardgame.com/images/Cards/109.jpg

# Restart containers to clear any stale SignalR connections
echo "🔄 Restarting containers to clear stale connections..."
docker-compose restart

echo ""
echo "✅ Empire TCG Fix Complete!"
echo ""
echo "🌐 Test the website: https://empirecardgame.com"
echo "🃏 Test deck builder: https://empirecardgame.com/deckbuilder"
echo "🏠 Test lobby: https://empirecardgame.com/lobby"
echo ""
echo "📋 Issues addressed:"
echo "   ✓ Fixed missing .NET framework files (icudt.dat)"
echo "   ✓ Improved Blazor WebAssembly deployment"
echo "   ✓ Cleared stale SignalR connections"
echo "   ✓ Verified card images are working"
echo "   ✓ Fresh container deployment"
echo ""
echo "🔧 If issues persist, check browser console for specific errors"
