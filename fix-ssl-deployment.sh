#!/bin/bash

echo "🔐 Empire TCG SSL Deployment Fix"
echo "================================"

# Function to check if domain resolves to current server
check_dns() {
    echo "🔍 Checking DNS resolution..."
    DOMAIN_IP=$(dig +short empirecardgame.com)
    WWW_DOMAIN_IP=$(dig +short www.empirecardgame.com)
    SERVER_IP=$(curl -s ifconfig.me)
    
    echo "Domain IP: $DOMAIN_IP"
    echo "WWW Domain IP: $WWW_DOMAIN_IP"
    echo "Server IP: $SERVER_IP"
    
    if [[ "$DOMAIN_IP" == "$SERVER_IP" ]] || [[ "$WWW_DOMAIN_IP" == "$SERVER_IP" ]]; then
        echo "✅ DNS is correctly configured"
        return 0
    else
        echo "⚠️  DNS may not be pointing to this server"
        echo "   Please ensure empirecardgame.com points to $SERVER_IP"
        read -p "Continue anyway? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            exit 1
        fi
    fi
}

# Step 1: Stop existing containers
echo "🛑 Stopping existing containers..."
docker-compose down

# Step 2: Check DNS
check_dns

# Step 3: Create SSL directory
echo "📁 Creating SSL directory..."
mkdir -p nginx/ssl

# Step 4: Use temporary nginx config (HTTP only)
echo "🔄 Switching to temporary HTTP-only nginx config..."
cp nginx/nginx-temp.conf nginx/nginx-current.conf

# Step 5: Update docker-compose to use temporary config
echo "📝 Updating docker-compose for temporary deployment..."
sed -i.bak 's|nginx.conf|nginx-current.conf|g' docker-compose.yml

# Step 6: Start containers with HTTP only
echo "🚀 Starting containers with HTTP-only configuration..."
docker-compose up -d empire-app nginx

# Step 7: Wait for services to be ready
echo "⏳ Waiting for services to start..."
sleep 15

# Step 8: Test HTTP access
echo "🌐 Testing HTTP access..."
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://empirecardgame.com || echo "000")
if [[ "$HTTP_STATUS" == "200" ]]; then
    echo "✅ HTTP access working!"
else
    echo "❌ HTTP access failed (Status: $HTTP_STATUS)"
    echo "Checking logs..."
    docker-compose logs nginx
    exit 1
fi

# Step 9: Generate SSL certificates
echo "🔒 Generating SSL certificates..."
docker-compose run --rm certbot

# Step 10: Check if certificates were generated
if [ -f "nginx/ssl/live/empirecardgame.com/fullchain.pem" ]; then
    echo "✅ SSL certificates generated successfully!"
    
    # Step 11: Switch back to full nginx config
    echo "🔄 Switching to full HTTPS nginx configuration..."
    cp nginx/nginx.conf nginx/nginx-current.conf
    
    # Step 12: Restart nginx with SSL
    echo "🔄 Restarting nginx with SSL..."
    docker-compose restart nginx
    
    # Step 13: Test HTTPS access
    echo "🔐 Testing HTTPS access..."
    sleep 10
    HTTPS_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://empirecardgame.com || echo "000")
    if [[ "$HTTPS_STATUS" == "200" ]]; then
        echo "✅ HTTPS access working!"
    else
        echo "⚠️  HTTPS access may need a moment to start (Status: $HTTPS_STATUS)"
    fi
    
    echo "🎉 SSL setup complete!"
    echo "🌐 Your site should now be accessible at:"
    echo "   https://empirecardgame.com"
    echo "   https://www.empirecardgame.com"
    
else
    echo "❌ SSL certificate generation failed!"
    echo "📋 Checking certbot logs..."
    docker-compose logs certbot
    echo ""
    echo "💡 Common issues:"
    echo "   - DNS not pointing to this server"
    echo "   - Port 80 not accessible from internet"
    echo "   - Domain not properly configured"
    exit 1
fi

# Step 14: Restore original docker-compose
echo "🔄 Restoring original docker-compose configuration..."
mv docker-compose.yml.bak docker-compose.yml

# Step 15: Show final status
echo "📊 Final container status:"
docker-compose ps

echo ""
echo "🔄 To renew certificates automatically, add this to crontab:"
echo "0 12 * * * cd $(pwd) && docker-compose run --rm certbot renew && docker-compose restart nginx"
