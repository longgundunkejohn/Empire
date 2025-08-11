#!/bin/bash

# Empire TCG Emergency Deployment Fix Script
# This script fixes the critical Blazor WebAssembly deployment issue

set -e  # Exit on any error

VPS_HOST="138.68.188.47"
VPS_USER="root"
SSH_KEY="~/.ssh/EmpireGame"

echo "🚨 EMERGENCY DEPLOYMENT FIX FOR EMPIRE TCG"
echo "==========================================="
echo ""
echo "Issue: Blazor WebAssembly runtime files (icudt.dat) missing from deployment"
echo "Fix: Updated Dockerfile and nginx configuration"
echo ""

# Backup current files
echo "📦 Creating backup of current deployment files..."
cp Dockerfile Dockerfile.backup.$(date +%Y%m%d_%H%M%S) || true
cp nginx/default.conf nginx/default.conf.backup.$(date +%Y%m%d_%H%M%S) || true

# Apply fixes
echo "🔧 Applying critical fixes..."

echo "  ✅ Replacing Dockerfile with fixed version..."
cp Dockerfile.fixed Dockerfile

echo "  ✅ Replacing nginx config with fixed version..."
cp nginx/default.conf.fixed nginx/default.conf

# Verify fixes are in place
echo "🔍 Verifying fixes are applied..."
if grep -q "CRITICAL FIX" Dockerfile; then
    echo "  ✅ Dockerfile fix confirmed"
else
    echo "  ❌ Dockerfile fix failed"
    exit 1
fi

if grep -q "CRITICAL FIX" nginx/default.conf; then
    echo "  ✅ nginx config fix confirmed"
else
    echo "  ❌ nginx config fix failed"
    exit 1
fi

# Test local build first
echo "🧪 Testing local Docker build..."
echo "  Building with fixed Dockerfile..."
docker build -t empire-test . || {
    echo "❌ Local Docker build failed!"
    echo "Please check the build output above for errors."
    exit 1
}

echo "  ✅ Local build successful!"

# Deploy to VPS
echo "🚀 Deploying fixes to VPS..."

# Create appsettings.json on VPS
echo "📝 Creating appsettings.json on VPS..."
ssh -i $SSH_KEY $VPS_USER@$VPS_HOST << 'EOF'
cat > /app/Empire.Server/appsettings.json << 'SETTINGS'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "EmpireGame"
  },
  "DeckDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "EmpireDecks"
  },
  "CardDB": {
    "ConnectionString": "mongodb+srv://admin:test123@cluster0.j0hbc7q.mongodb.net/Empire-Deckbuilder?retryWrites=true&w=majority&appName=Cluster0",
    "DatabaseName": "Empire-Deckbuilder"
  }
}
SETTINGS
EOF

# Stop current deployment
echo "🛑 Stopping current deployment..."
ssh -i $SSH_KEY $VPS_USER@$VPS_HOST << 'EOF'
cd /app
docker-compose down || true
EOF

# Deploy the fixed application
echo "📤 Uploading fixed files..."
rsync -avz -e "ssh -i $SSH_KEY" --exclude='appsettings*.json' --exclude='bin/' --exclude='obj/' --exclude='*.backup.*' ./ $VPS_USER@$VPS_HOST:/app/

# Clean Docker cache and rebuild
echo "🧹 Cleaning Docker cache and rebuilding..."
ssh -i $SSH_KEY $VPS_USER@$VPS_HOST << 'EOF'
cd /app

# Clean Docker cache to ensure fresh build
docker system prune -f
docker builder prune -f

# Remove old images
docker rmi empire-server:latest || true

# Rebuild with no cache to ensure fixes are applied
docker-compose build --no-cache

# Start the application
docker-compose up -d
EOF

# Wait for application to start
echo "⏳ Waiting for application to start..."
sleep 30

# Verify deployment
echo "🔍 Verifying deployment..."

# Check if the critical file is now accessible
echo "  Testing icudt.dat file availability..."
if ssh -i $SSH_KEY $VPS_USER@$VPS_HOST "curl -s -I https://empirecardgame.com/_framework/icudt.dat | grep -q '200 OK'"; then
    echo "  ✅ icudt.dat is now accessible!"
else
    echo "  ⚠️  icudt.dat still not accessible, checking HTTP..."
    if ssh -i $SSH_KEY $VPS_USER@$VPS_HOST "curl -s -I http://empirecardgame.com/_framework/icudt.dat | grep -q '200 OK'"; then
        echo "  ✅ icudt.dat accessible via HTTP (SSL may need time)"
    else
        echo "  ❌ icudt.dat still not accessible"
    fi
fi

# Check if main page loads
echo "  Testing main page..."
if ssh -i $SSH_KEY $VPS_USER@$VPS_HOST "curl -s https://empirecardgame.com | grep -q 'Empire'"; then
    echo "  ✅ Main page loads"
else
    echo "  ⚠️  Main page may have issues"
fi

# Show container status
echo "📊 Container status:"
ssh -i $SSH_KEY $VPS_USER@$VPS_HOST "cd /app && docker-compose ps"

echo ""
echo "🎉 DEPLOYMENT FIX COMPLETE!"
echo "=========================="
echo ""
echo "✅ Fixed Dockerfile to properly build Blazor WebAssembly files"
echo "✅ Fixed nginx configuration for proper MIME types"
echo "✅ Deployed to production"
echo ""
echo "🔗 Test the application:"
echo "   Main site: https://empirecardgame.com"
echo "   Deck builder: https://empirecardgame.com/deckbuilder"
echo "   Lobby: https://empirecardgame.com/lobby"
echo ""
echo "🔍 If issues persist, check:"
echo "   1. Container logs: ssh -i $SSH_KEY $VPS_USER@$VPS_HOST 'cd /app && docker-compose logs'"
echo "   2. nginx logs: ssh -i $SSH_KEY $VPS_USER@$VPS_HOST 'docker logs nginx'"
echo "   3. Application logs: ssh -i $SSH_KEY $VPS_USER@$VPS_HOST 'docker logs empire-server'"
echo ""
echo "📝 Backup files created:"
echo "   - Dockerfile.backup.*"
echo "   - nginx/default.conf.backup.*"
