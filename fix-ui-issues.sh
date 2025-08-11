#!/bin/bash

echo "🔧 Fixing UI issues: Home button removal and deck upload functionality..."

# Step 1: Pull the latest changes
echo "📥 Pulling latest changes..."
git pull origin main

# Step 2: Rebuild and restart the application
echo "🔄 Rebuilding and restarting application..."
docker-compose down
docker-compose up --build -d

# Step 3: Check container status
echo "📊 Checking container status..."
docker-compose ps

# Step 4: Check logs for any errors
echo "📋 Checking recent logs..."
docker-compose logs --tail=20 empire-server

echo "✅ UI fixes applied!"
echo ""
echo "🎯 Changes made:"
echo "   ✅ Removed unnecessary Home button from navigation"
echo "   ✅ Enhanced deck upload with better error handling"
echo "   ✅ Increased file upload limits to 10MB"
echo "   ✅ Added detailed logging for upload debugging"
echo ""
echo "🌐 Test the fixes at:"
echo "   - https://empirecardgame.com/lobby"
echo ""
echo "📝 Deck upload should now work properly with CSV files up to 10MB"
echo "📝 Home button should no longer appear in the navigation menu"
