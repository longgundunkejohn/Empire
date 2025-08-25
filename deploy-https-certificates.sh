#!/bin/bash

echo "?? EMPIRE TCG HTTPS CERTIFICATE DEPLOYMENT"
echo "=========================================="

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

# Step 1: Create certificates directory structure
print_status "Creating certificate directory structure..."
mkdir -p certbot/conf/live/empirecardgame.com
mkdir -p certbot/www/.well-known/acme-challenge
chmod -R 755 certbot/

# Step 2: Create temporary HTTP-only nginx config for ACME challenge
print_status "Creating temporary nginx configuration for certificate acquisition..."

cat > nginx/empire-cms-acme.conf << 'EOF'
server {
    listen 80;
    server_name empirecardgame.com www.empirecardgame.com;

    # ACME Challenge for Let's Encrypt
    location /.well-known/acme-challenge/ {
        root /var/www/certbot;
        try_files $uri =404;
        allow all;
    }

    # Temporary pass-through for services during certificate acquisition
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

    location /gamehub/ {
        proxy_pass http://empire-tcg-game:8080/gamehub/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto http;
        
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_cache_bypass $http_upgrade;
    }

    location /wp-json/ {
        proxy_pass http://empire-wordpress:80;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto http;
    }
}
EOF

print_success "Created temporary ACME configuration"

# Step 3: Update docker-compose to use ACME config temporarily
print_status "Switching to ACME challenge configuration..."
cp docker-compose-cms.yml docker-compose-cms-backup.yml
sed -i 's|./nginx/empire-cms.conf|./nginx/empire-cms-acme.conf|g' docker-compose-cms.yml

# Step 4: Start services for certificate acquisition
print_status "Starting services with ACME configuration..."
docker-compose -f docker-compose-cms.yml down
docker-compose -f docker-compose-cms.yml up -d mysql wordpress empire-game nginx

# Wait for services
print_status "Waiting for services to initialize..."
sleep 45

# Step 5: Test ACME challenge endpoint
print_status "Testing ACME challenge endpoint..."
ACME_TEST=$(curl -s -o /dev/null -w "%{http_code}" http://empirecardgame.com/.well-known/acme-challenge/test 2>/dev/null || echo "000")

if [ "$ACME_TEST" = "404" ]; then
    print_success "ACME challenge endpoint responding correctly"
elif [ "$ACME_TEST" = "200" ]; then
    print_success "ACME challenge endpoint accessible"
else
    print_warning "ACME challenge test returned: $ACME_TEST"
    print_warning "Proceeding anyway - Let's Encrypt will test with its own file"
fi

# Step 6: Generate SSL certificates
print_status "Requesting SSL certificates from Let's Encrypt..."
print_warning "Domain must point to this server! Checking DNS..."

DOMAIN_IP=$(dig +short empirecardgame.com | tail -n1)
SERVER_IP=$(curl -s ifconfig.me 2>/dev/null || curl -s ipinfo.io/ip 2>/dev/null)

if [ "$DOMAIN_IP" = "$SERVER_IP" ]; then
    print_success "DNS correctly points to this server ($SERVER_IP)"
else
    print_warning "DNS mismatch: empirecardgame.com -> $DOMAIN_IP, server -> $SERVER_IP"
    print_warning "This may cause certificate generation to fail"
fi

# Request certificates
docker-compose -f docker-compose-cms.yml run --rm certbot \
    certonly --webroot --webroot-path=/var/www/certbot \
    --email oskarmelorm@gmail.com --agree-tos --no-eff-email \
    --force-renewal --verbose \
    -d empirecardgame.com -d www.empirecardgame.com

CERT_STATUS=$?

if [ $CERT_STATUS -eq 0 ]; then
    print_success "?? SSL certificates generated successfully!"
    
    # Step 7: Verify certificates exist
    if [ -f "certbot/conf/live/empirecardgame.com/fullchain.pem" ] && [ -f "certbot/conf/live/empirecardgame.com/privkey.pem" ]; then
        print_success "Certificate files verified"
        
        # Step 8: Restore original HTTPS configuration
        print_status "Switching to HTTPS configuration..."
        cp docker-compose-cms-backup.yml docker-compose-cms.yml
        
        # Step 9: Restart nginx with HTTPS
        print_status "Restarting nginx with HTTPS configuration..."
        docker-compose -f docker-compose-cms.yml restart nginx
        
        # Wait for nginx to reload
        sleep 15
        
        # Step 10: Test HTTPS
        print_status "Testing HTTPS connectivity..."
        HTTPS_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://empirecardgame.com 2>/dev/null || echo "000")
        
        if [ "$HTTPS_STATUS" = "200" ] || [ "$HTTPS_STATUS" = "302" ] || [ "$HTTPS_STATUS" = "301" ]; then
            print_success "? HTTPS is working! ($HTTPS_STATUS)"
            
            # Test WebSocket over HTTPS
            print_status "Testing SignalR WebSocket over HTTPS..."
            WS_TEST=$(curl -s -o /dev/null -w "%{http_code}" https://empirecardgame.com/gamehub/ 2>/dev/null || echo "000")
            if [ "$WS_TEST" = "404" ] || [ "$WS_TEST" = "400" ]; then
                print_success "SignalR endpoint accessible over HTTPS"
            fi
            
            echo ""
            print_success "?? EMPIRE TCG HTTPS DEPLOYMENT COMPLETE!"
            echo ""
            echo "? Your Empire TCG platform is now secure and accessible at:"
            echo "   ?? https://empirecardgame.com"
            echo "   ?? https://www.empirecardgame.com"
            echo "   ?? https://empirecardgame.com/play/"
            echo "   ?? https://empirecardgame.com/gamehub/ (SignalR)"
            echo ""
            echo "?? SSL Certificate Information:"
            echo "   ?? Issuer: Let's Encrypt"
            echo "   ?? Valid for: 90 days"
            echo "   ?? Auto-renewal recommended"
            echo ""
            echo "?? Certificate Management:"
            echo "   ?? Check status: ./check-certificates.sh"
            echo "   ?? Renew: ./renew-certificates.sh"
            echo "   ?? Auto-renewal cron: 0 2 * * 1 /path/to/renew-certificates.sh"
            
        else
            print_error "HTTPS test failed (status: $HTTPS_STATUS)"
            print_error "Certificates generated but HTTPS configuration may have issues"
            
            # Show nginx logs for debugging
            echo ""
            print_status "Nginx logs for debugging:"
            docker-compose -f docker-compose-cms.yml logs --tail=20 nginx
        fi
        
    else
        print_error "Certificate files not found after generation"
        print_error "Check certbot logs: docker-compose -f docker-compose-cms.yml logs certbot"
    fi
    
else
    print_error "? SSL certificate generation failed!"
    echo ""
    print_error "Common causes:"
    echo "1. ?? Domain doesn't point to this server"
    echo "2. ?? Firewall blocking port 80"
    echo "3. ??  Rate limiting (too many requests)"
    echo "4. ?? Port 80 not accessible from internet"
    echo ""
    print_status "Showing certbot logs..."
    docker-compose -f docker-compose-cms.yml logs certbot
    
    # Restore HTTP config for troubleshooting
    print_status "Keeping HTTP configuration for troubleshooting..."
    
    exit 1
fi

# Clean up temporary files
rm -f nginx/empire-cms-acme.conf docker-compose-cms-backup.yml

print_success "?? HTTPS setup complete! Your Blazor WebAssembly client is now properly served over HTTPS."