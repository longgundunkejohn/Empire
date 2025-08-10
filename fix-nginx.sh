#!/bin/bash

echo "🔧 Fixing nginx configuration to properly serve Blazor app..."

# Step 1: Pull the latest nginx configuration
echo "📥 Pulling latest nginx configuration..."
git pull origin main

# Step 2: Restart nginx to apply the new configuration
echo "🔄 Restarting nginx with new configuration..."
docker-compose restart nginx

# Step 3: Check if containers are running
echo "📊 Checking container status..."
docker-compose ps

# Step 4: Test the configuration
echo "🧪 Testing nginx configuration..."
docker exec nginx nginx -t

echo "✅ Nginx configuration updated!"
echo ""
echo "🌐 Your Blazor TCG application should now be accessible at:"
echo "   - http://empirecardgame.com"
echo "   - https://empirecardgame.com"
echo ""
echo "The site should now show your Empire Card Game instead of the nginx welcome page!"
