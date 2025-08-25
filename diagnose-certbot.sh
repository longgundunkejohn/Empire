#!/bin/bash

echo "?? CERTBOT DIAGNOSTIC & FIX"
echo "============================"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

print_status() { echo -e "${BLUE}[INFO]${NC} $1"; }
print_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
print_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
print_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Step 1: Detailed diagnostics
print_status "Running detailed ACME challenge diagnostics..."

# Check if nginx is running and accessible
print_status "Testing nginx accessibility..."
curl -v http://empirecardgame.com 2>&1 | head -20

echo ""
print_status "Testing ACME challenge path specifically..."
# Create a test file
docker-compose -f docker-compose-cms.yml exec nginx mkdir -p /var/www/certbot/.well-known/acme-challenge
docker-compose -f docker-compose-cms.yml exec nginx sh -c 'echo "test-file-content" > /var/www/certbot/.well-known/acme-challenge/test-file'

# Test if we can access it
TEST_RESULT=$(curl -s http://empirecardgame.com/.well-known/acme-challenge/test-file 2>/dev/null)
if [ "$TEST_RESULT" = "test-file-content" ]; then
    print_success "ACME challenge path is working correctly"
else
    print_error "ACME challenge path not accessible. Got: '$TEST_RESULT'"
    
    # Check nginx config
    print_status "Checking nginx configuration..."
    docker-compose -f docker-compose-cms.yml exec nginx cat /etc/nginx/conf.d/default.conf | grep -A5 -B5 "well-known"
fi

# Step 2: Check certbot logs in detail
print_status "Examining certbot failure details..."
docker-compose -f docker-compose-cms.yml logs certbot 2>&1 | tail -30

# Step 3: Test Let's Encrypt connectivity
print_status "Testing Let's Encrypt server connectivity..."
docker-compose -f docker-compose-cms.yml run --rm certbot --version

# Step 4: Check rate limiting
print_status "Checking for rate limiting issues..."
docker-compose -f docker-compose-cms.yml run --rm certbot certificates

# Step 5: Try with staging server first
print_status "Attempting certificate generation with Let's Encrypt staging server..."
docker-compose -f docker-compose-cms.yml run --rm certbot \
    certonly --webroot --webroot-path=/var/www/certbot \
    --email oskarmelorm@gmail.com --agree-tos --no-eff-email \
    --staging --force-renewal --verbose \
    -d empirecardgame.com -d www.empirecardgame.com

STAGING_RESULT=$?

if [ $STAGING_RESULT -eq 0 ]; then
    print_success "Staging certificates work! Rate limiting or server issue with production."
    
    print_status "Now trying production certificates..."
    docker-compose -f docker-compose-cms.yml run --rm certbot \
        certonly --webroot --webroot-path=/var/www/certbot \
        --email oskarmelorm@gmail.com --agree-tos --no-eff-email \
        --force-renewal --verbose \
        -d empirecardgame.com -d www.empirecardgame.com
    
    PROD_RESULT=$?
    
    if [ $PROD_RESULT -eq 0 ]; then
        print_success "?? Production certificates generated successfully!"
        
        # Switch to HTTPS config
        print_status "Switching to HTTPS configuration..."
        cp docker-compose-cms.yml docker-compose-cms-backup.yml
        sed -i 's|./nginx/empire-cms-acme.conf|./nginx/empire-cms.conf|g' docker-compose-cms.yml
        docker-compose -f docker-compose-cms.yml restart nginx
        
        # Test HTTPS
        sleep 15
        HTTPS_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://empirecardgame.com 2>/dev/null || echo "000")
        
        if [ "$HTTPS_STATUS" = "200" ] || [ "$HTTPS_STATUS" = "302" ] || [ "$HTTPS_STATUS" = "301" ]; then
            print_success "? HTTPS working! (status: $HTTPS_STATUS)"
            print_success "?? Empire TCG is now fully HTTPS enabled!"
            echo ""
            echo "?? Access your platform at:"
            echo "   https://empirecardgame.com"
            echo "   https://empirecardgame.com/play/"
            echo ""
        else
            print_error "HTTPS not working (status: $HTTPS_STATUS)"
        fi
        
    else
        print_error "Production certificate generation still failed"
        print_status "This suggests rate limiting. Wait 1 hour and try again."
    fi
    
else
    print_error "Even staging certificates failed. There's a fundamental configuration issue."
    
    # Enhanced debugging
    print_status "Enhanced debugging - checking container networking..."
    docker-compose -f docker-compose-cms.yml exec nginx ls -la /var/www/certbot/.well-known/acme-challenge/
    
    print_status "Checking if nginx can reach the challenge directory..."
    docker-compose -f docker-compose-cms.yml exec nginx ls -la /var/www/certbot/
    
    print_status "Testing internal container connectivity..."
    docker-compose -f docker-compose-cms.yml exec nginx wget -qO- http://localhost/.well-known/acme-challenge/test-file || echo "Internal access failed"
    
fi

echo ""
print_status "?? Diagnostic complete. Check the output above for specific issues."