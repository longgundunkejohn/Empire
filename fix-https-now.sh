#!/bin/bash

echo "🔧 Quick HTTPS Fix for Empire TCG"
echo "================================="

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Test nginx config
print_status "Testing current nginx configuration..."
docker exec empire-nginx nginx -t

if [[ $? -ne 0 ]]; then
    print_error "Nginx config has syntax errors!"
    exit 1
fi

print_success "Nginx config syntax is valid"

# Check if SSL files are accessible inside container
print_status "Checking SSL certificate files inside nginx container..."
docker exec empire-nginx ls -la /etc/nginx/ssl/live/empirecardgame.com/

# Test SSL certificate validity
print_status "Testing SSL certificate validity..."
docker exec empire-nginx openssl x509 -in /etc/nginx/ssl/live/empirecardgame.com/fullchain.pem -text -noout | head -20

# Reload nginx configuration
print_status "Reloading nginx configuration..."
docker exec empire-nginx nginx -s reload

# Wait a moment
sleep 5

# Test HTTPS connectivity again
print_status "Testing HTTPS connectivity after reload..."
HTTPS_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://empirecardgame.com || echo "000")
print_status "HTTPS Status: $HTTPS_STATUS"

if [[ "$HTTPS_STATUS" == "200" ]]; then
    print_success "🎉 HTTPS is now working!"
    print_success "Site accessible at: https://empirecardgame.com"
    print_success "Lobby accessible at: https://empirecardgame.com/lobby"
else
    print_error "HTTPS still not working. Let's check detailed nginx logs..."
    docker exec empire-nginx cat /var/log/nginx/error.log | tail -10
    
    print_status "Checking if nginx is listening on port 443..."
    docker exec empire-nginx netstat -tlnp | grep :443
    
    print_status "Checking nginx processes..."
    docker exec empire-nginx ps aux | grep nginx
fi

# Final test with verbose curl
print_status "Testing with verbose curl..."
curl -v https://empirecardgame.com 2>&1 | head -20
