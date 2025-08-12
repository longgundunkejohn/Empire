#!/bin/bash

echo "🔐 Empire TCG SSL Deployment Fix v2.0"
echo "====================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to check if domain resolves to current server
check_dns() {
    print_status "Checking DNS resolution..."
    DOMAIN_IP=$(dig +short empirecardgame.com)
    WWW_DOMAIN_IP=$(dig +short www.empirecardgame.com)
    SERVER_IP=$(curl -s ifconfig.me)
    
    echo "Domain IP: $DOMAIN_IP"
    echo "WWW Domain IP: $WWW_DOMAIN_IP"
    echo "Server IP: $SERVER_IP"
    
    if [[ "$DOMAIN_IP" == "$SERVER_IP" ]] && [[ "$WWW_DOMAIN_IP" == "$SERVER_IP" ]]; then
        print_success "DNS is correctly configured for both domains"
        return 0
    elif [[ "$DOMAIN_IP" == "$SERVER_IP" ]] || [[ "$WWW_DOMAIN_IP" == "$SERVER_IP" ]]; then
        print_warning "Only one domain is pointing to this server"
        echo "   empirecardgame.com -> $DOMAIN_IP"
        echo "   www.empirecardgame.com -> $WWW_DOMAIN_IP"
        echo "   Expected: $SERVER_IP"
        read -p "Continue anyway? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            exit 1
        fi
    else
        print_error "DNS is not pointing to this server"
        echo "   Please ensure both domains point to $SERVER_IP"
        read -p "Continue anyway? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            exit 1
        fi
    fi
}

# Function to test challenge directory access
test_challenge_access() {
    print_status "Testing challenge directory access..."
    
    # Create test file
    mkdir -p /tmp/certbot-test/.well-known/acme-challenge/
    echo "test-challenge-file" > /tmp/certbot-test/.well-known/acme-challenge/test-file
    
    # Start temporary nginx with test config
    docker run --rm -d \
        --name nginx-test \
        -p 8080:80 \
        -v /tmp/certbot-test:/var/www/certbot:ro \
        -v $(pwd)/nginx/nginx-temp.conf:/etc/nginx/nginx.conf:ro \
        nginx:alpine
    
    sleep 3
    
    # Test access
    HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/.well-known/acme-challenge/test-file || echo "000")
    
    # Cleanup
    docker stop nginx-test 2>/dev/null || true
    rm -rf /tmp/certbot-test
    
    if [[ "$HTTP_STATUS" == "200" ]]; then
        print_success "Challenge directory access test passed"
        return 0
    else
        print_error "Challenge directory access test failed (Status: $HTTP_STATUS)"
        return 1
    fi
}

# Step 1: Stop existing containers
print_status "Stopping existing containers..."
docker-compose down

# Step 2: Check DNS
check_dns

# Step 3: Create SSL directory
print_status "Creating SSL directory..."
mkdir -p nginx/ssl

# Step 4: Test challenge access (optional)
print_status "Would you like to test challenge directory access first? (recommended)"
read -p "Test challenge access? (Y/n): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Nn]$ ]]; then
    if ! test_challenge_access; then
        print_warning "Challenge access test failed, but continuing anyway..."
    fi
fi

# Step 5: Use temporary nginx config (HTTP only)
print_status "Switching to temporary HTTP-only nginx config..."
cp nginx/nginx-temp.conf nginx/nginx-current.conf

# Step 6: Update docker-compose to use temporary config
print_status "Updating docker-compose for temporary deployment..."
sed -i.bak 's|nginx.conf|nginx-current.conf|g' docker-compose.yml

# Step 7: Start containers with HTTP only
print_status "Starting containers with HTTP-only configuration..."
docker-compose up -d empire-app nginx

# Step 8: Wait for services to be ready
print_status "Waiting for services to start..."
for i in {1..30}; do
    if docker-compose ps | grep -q "Up"; then
        break
    fi
    echo -n "."
    sleep 1
done
echo

# Step 9: Test HTTP access
print_status "Testing HTTP access..."
sleep 5
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://empirecardgame.com || echo "000")
if [[ "$HTTP_STATUS" == "200" ]]; then
    print_success "HTTP access working!"
else
    print_error "HTTP access failed (Status: $HTTP_STATUS)"
    print_status "Checking nginx logs..."
    docker-compose logs nginx | tail -20
    print_status "Checking app logs..."
    docker-compose logs empire-tcg | tail -10
    exit 1
fi

# Step 10: Test challenge directory specifically
print_status "Testing challenge directory access..."
CHALLENGE_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://empirecardgame.com/.well-known/acme-challenge/ || echo "000")
if [[ "$CHALLENGE_STATUS" == "404" ]]; then
    print_success "Challenge directory returns 404 (expected for empty directory)"
elif [[ "$CHALLENGE_STATUS" == "403" ]]; then
    print_success "Challenge directory returns 403 (acceptable)"
else
    print_warning "Challenge directory returns unexpected status: $CHALLENGE_STATUS"
fi

# Step 11: Generate SSL certificates
print_status "Generating SSL certificates..."
print_status "This may take a few minutes..."

if docker-compose run --rm certbot; then
    print_success "Certificate generation completed"
else
    print_error "Certificate generation failed"
    print_status "Checking certbot logs..."
    docker-compose logs certbot
    
    print_status "Checking if challenge files were created..."
    docker exec empire-nginx ls -la /var/www/certbot/.well-known/acme-challenge/ 2>/dev/null || echo "Challenge directory not accessible"
    
    print_status "Testing challenge URL manually..."
    curl -v http://empirecardgame.com/.well-known/acme-challenge/test 2>&1 | head -20
    
    exit 1
fi

# Step 12: Check if certificates were generated
if [ -f "nginx/ssl/live/empirecardgame.com/fullchain.pem" ]; then
    print_success "SSL certificates generated successfully!"
    
    # Step 13: Switch back to full nginx config
    print_status "Switching to full HTTPS nginx configuration..."
    cp nginx/nginx.conf nginx/nginx-current.conf
    
    # Step 14: Restart nginx with SSL
    print_status "Restarting nginx with SSL..."
    docker-compose restart nginx
    
    # Step 15: Test HTTPS access
    print_status "Testing HTTPS access..."
    sleep 10
    
    for domain in "empirecardgame.com" "www.empirecardgame.com"; do
        HTTPS_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://$domain || echo "000")
        if [[ "$HTTPS_STATUS" == "200" ]]; then
            print_success "HTTPS access working for $domain!"
        else
            print_warning "HTTPS access may need a moment for $domain (Status: $HTTPS_STATUS)"
        fi
    done
    
    print_success "SSL setup complete!"
    echo "🌐 Your site should now be accessible at:"
    echo "   https://empirecardgame.com"
    echo "   https://www.empirecardgame.com"
    
else
    print_error "SSL certificate files not found!"
    print_status "Expected location: nginx/ssl/live/empirecardgame.com/"
    print_status "Checking what was created..."
    find nginx/ssl -type f -name "*.pem" 2>/dev/null || echo "No certificate files found"
    exit 1
fi

# Step 16: Restore original docker-compose
print_status "Restoring original docker-compose configuration..."
mv docker-compose.yml.bak docker-compose.yml

# Step 17: Show final status
print_status "Final container status:"
docker-compose ps

echo ""
print_success "🎉 SSL deployment completed successfully!"
echo ""
print_status "To set up automatic certificate renewal, run:"
echo "crontab -e"
echo "Then add this line:"
echo "0 12 * * * cd $(pwd) && docker-compose run --rm certbot renew && docker-compose restart nginx"
echo ""
print_status "To test certificate renewal:"
echo "docker-compose run --rm certbot renew --dry-run"
