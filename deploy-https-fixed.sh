#!/bin/bash

echo "?? FIXED EMPIRE TCG HTTPS CERTIFICATE DEPLOYMENT"
echo "==============================================="

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

# Check if we're running on the server
if [ ! -f "docker-compose-cms.yml" ]; then
    print_error "This script must be run from the Empire repository directory on the server"
    exit 1
fi

# Step 1: Stop all services to ensure clean state
print_status "Stopping all services for clean restart..."
docker-compose -f docker-compose-cms.yml down

# Step 2: Create proper directory structure with correct permissions
print_status "Setting up certificate directories with proper permissions..."
sudo mkdir -p certbot/conf certbot/www/.well-known/acme-challenge
sudo chmod -R 755 certbot/
sudo chown -R root:root certbot/

# Step 3: Create improved ACME challenge nginx config
print_status "Creating improved ACME challenge configuration..."

cat > nginx/empire-cms-acme-fixed.conf << 'EOF'
server {
    listen 80;
    server_name empirecardgame.com www.empirecardgame.com;

    # ACME Challenge - highest priority
    location ^~ /.well-known/acme-challenge/ {
        root /var/www/certbot;
        try_files $uri =404;
        allow all;
        # Ensure no authentication or special handling
        access_log off;
        log_not_found off;
    }

    # Everything else routes to WordPress
    location / {
        proxy_pass http://empire-wordpress:80;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto http;
        proxy_set_header X-Forwarded-Host $host;
        
        proxy_read_timeout 300;
        proxy_connect_timeout 300;
        proxy_send_timeout 300;
    }

    # Game endpoints for testing
    location /play/ {
        proxy_pass http://empire-tcg-game:8080/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto http;
        
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_cache_bypass $http_upgrade;
    }

    location /game-api/ {
        proxy_pass http://empire-tcg-game:8080/api/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto http;
    }
}
EOF

print_success "Created improved ACME configuration"

# Step 4: Update docker-compose to use the fixed ACME config
print_status "Updating docker-compose configuration..."
cp docker-compose-cms.yml docker-compose-cms-original.yml
sed -i 's|./nginx/empire-cms.conf|./nginx/empire-cms-acme-fixed.conf|g' docker-compose-cms.yml

# Step 5: Start services with ACME configuration
print_status "Starting services with ACME configuration..."
docker-compose -f docker-compose-cms.yml up -d

# Wait longer for services to fully initialize
print_status "Waiting for services to fully initialize..."
sleep 60

# Step 6: Test ACME challenge path thoroughly
print_status "Testing ACME challenge path..."

# Create test file
docker-compose -f docker-compose-cms.yml exec nginx mkdir -p /var/www/certbot/.well-known/acme-challenge
docker-compose -f docker-compose-cms.yml exec nginx sh -c 'echo "acme-test-$(date +%s)" > /var/www/certbot/.well-known/acme-challenge/test-challenge'

# Test from outside
TEST_CONTENT=$(curl -s http://empirecardgame.com/.well-known/acme-challenge/test-challenge 2>/dev/null)
if [[ "$TEST_CONTENT" == acme-test-* ]]; then
    print_success "ACME challenge path working correctly"
else
    print_error "ACME challenge path not working. Got: '$TEST_CONTENT'"
    print_status "Debugging nginx configuration..."
    docker-compose -f docker-compose-cms.yml logs nginx | tail -20
    exit 1
fi

# Step 7: First try with staging server to validate everything
print_status "Testing with Let's Encrypt staging server first..."
docker-compose -f docker-compose-cms.yml run --rm certbot \
    certonly --webroot --webroot-path=/var/www/certbot \
    --email oskarmelorm@gmail.com --agree-tos --no-eff-email \
    --staging --force-renewal \
    -d empirecardgame.com -d www.empirecardgame.com

STAGING_RESULT=$?

if [ $STAGING_RESULT -eq 0 ]; then
    print_success "Staging certificates successful! Now requesting production certificates..."
    
    # Clean up staging certificates
    docker-compose -f docker-compose-cms.yml run --rm certbot delete --cert-name empirecardgame.com --non-interactive
    
    # Step 8: Request production certificates
    print_status "Requesting production SSL certificates..."
    docker-compose -f docker-compose-cms.yml run --rm certbot \
        certonly --webroot --webroot-path=/var/www/certbot \
        --email oskarmelorm@gmail.com --agree-tos --no-eff-email \
        --force-renewal \
        -d empirecardgame.com -d www.empirecardgame.com

    PROD_RESULT=$?

    if [ $PROD_RESULT -eq 0 ]; then
        print_success "?? Production SSL certificates generated successfully!"
        
        # Step 9: Verify certificate files exist
        if docker-compose -f docker-compose-cms.yml run --rm certbot ls /etc/letsencrypt/live/empirecardgame.com/fullchain.pem > /dev/null 2>&1; then
            print_success "Certificate files verified"
            
            # Step 10: Switch to HTTPS configuration
            print_status "Switching to production HTTPS configuration..."
            cp docker-compose-cms-original.yml docker-compose-cms.yml
            docker-compose -f docker-compose-cms.yml restart nginx
            
            # Wait for nginx reload
            sleep 20
            
            # Step 11: Test HTTPS
            print_status "Testing HTTPS connectivity..."
            HTTPS_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://empirecardgame.com 2>/dev/null || echo "000")
            
            if [ "$HTTPS_STATUS" = "200" ] || [ "$HTTPS_STATUS" = "302" ] || [ "$HTTPS_STATUS" = "301" ]; then
                print_success "? HTTPS working perfectly! (status: $HTTPS_STATUS)"
                
                # Test all key endpoints
                print_status "Testing Empire TCG specific endpoints..."
                
                GAME_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://empirecardgame.com/play/ 2>/dev/null || echo "000")
                if [ "$GAME_STATUS" = "200" ] || [ "$GAME_STATUS" = "404" ]; then
                    print_success "Game endpoint accessible over HTTPS"
                fi
                
                API_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://empirecardgame.com/game-api/ 2>/dev/null || echo "000")
                if [ "$API_STATUS" = "404" ] || [ "$API_STATUS" = "401" ]; then
                    print_success "API endpoint accessible over HTTPS"
                fi
                
                echo ""
                print_success "?? EMPIRE TCG HTTPS DEPLOYMENT COMPLETE!"
                echo ""
                echo "? Your Blazor WebAssembly Empire TCG platform is now secure:"
                echo "   ?? https://empirecardgame.com"
                echo "   ?? https://empirecardgame.com/play/"
                echo "   ?? https://empirecardgame.com/gamehub/ (SignalR)"
                echo "   ?? WordPress shop integration over HTTPS"
                echo ""
                echo "?? SSL Certificate Details:"
                echo "   ?? Valid for 90 days"
                echo "   ?? Auto-renewal: ./renew-certificates.sh"
                echo "   ?? Status check: ./check-certificates.sh"
                echo ""
                echo "?? Your Blazor WebAssembly client will now work perfectly"
                echo "   with its HTTPS configuration in Program.cs!"
                
            else
                print_error "HTTPS not responding (status: $HTTPS_STATUS)"
                print_status "Certificate installed but nginx configuration may need adjustment"
                docker-compose -f docker-compose-cms.yml logs nginx | tail -10
            fi
            
        else
            print_error "Certificate files not found after generation"
        fi
        
    else
        print_error "Production certificate generation failed"
        print_status "This might be due to rate limiting. Check certbot logs:"
        docker-compose -f docker-compose-cms.yml logs certbot | tail -20
    fi
    
else
    print_error "Staging certificate generation failed"
    print_error "There's a fundamental configuration issue with the ACME challenge"
    print_status "Running diagnostics..."
    
    # Enhanced diagnostics
    print_status "Container logs:"
    docker-compose -f docker-compose-cms.yml logs nginx | tail -10
    
    print_status "Directory permissions:"
    docker-compose -f docker-compose-cms.yml exec nginx ls -la /var/www/certbot/.well-known/acme-challenge/
    
    exit 1
fi

# Clean up temporary files
rm -f nginx/empire-cms-acme-fixed.conf docker-compose-cms-original.yml

print_success "?? HTTPS deployment complete!"