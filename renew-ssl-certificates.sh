#!/bin/bash

echo "🔄 Empire TCG SSL Certificate Renewal"
echo "===================================="

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if we're in the right directory
if [ ! -f "docker-compose.yml" ]; then
    print_error "docker-compose.yml not found. Please run this script from the Empire project directory."
    exit 1
fi

# Check certificate expiration
echo "📅 Checking current certificate expiration..."
if [ -f "nginx/ssl/live/empirecardgame.com/fullchain.pem" ]; then
    EXPIRY_DATE=$(openssl x509 -enddate -noout -in nginx/ssl/live/empirecardgame.com/fullchain.pem | cut -d= -f2)
    echo "Current certificate expires: $EXPIRY_DATE"
    
    # Check if certificate expires in less than 30 days
    EXPIRY_EPOCH=$(date -d "$EXPIRY_DATE" +%s)
    CURRENT_EPOCH=$(date +%s)
    DAYS_UNTIL_EXPIRY=$(( (EXPIRY_EPOCH - CURRENT_EPOCH) / 86400 ))
    
    if [ $DAYS_UNTIL_EXPIRY -gt 30 ]; then
        print_success "Certificate is still valid for $DAYS_UNTIL_EXPIRY days. No renewal needed."
        echo "Use --force to renew anyway."
        if [[ "$1" != "--force" ]]; then
            exit 0
        fi
    else
        print_warning "Certificate expires in $DAYS_UNTIL_EXPIRY days. Renewal recommended."
    fi
else
    print_error "No existing certificate found. Please run the initial SSL setup first."
    exit 1
fi

# Perform renewal
echo "🔄 Attempting certificate renewal..."
if docker-compose run --rm certbot renew; then
    print_success "Certificate renewal completed successfully!"
    
    # Restart nginx to use new certificates
    echo "🔄 Restarting nginx to use new certificates..."
    docker-compose restart nginx
    
    # Verify the new certificate
    sleep 5
    NEW_EXPIRY_DATE=$(openssl x509 -enddate -noout -in nginx/ssl/live/empirecardgame.com/fullchain.pem | cut -d= -f2)
    echo "New certificate expires: $NEW_EXPIRY_DATE"
    
    # Test HTTPS access
    echo "🔐 Testing HTTPS access..."
    for domain in "empirecardgame.com" "www.empirecardgame.com"; do
        HTTPS_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://$domain || echo "000")
        if [[ "$HTTPS_STATUS" == "200" ]]; then
            print_success "HTTPS working for $domain"
        else
            print_warning "HTTPS may need a moment for $domain (Status: $HTTPS_STATUS)"
        fi
    done
    
    print_success "SSL certificate renewal completed successfully!"
    
else
    print_error "Certificate renewal failed!"
    echo "📋 Checking logs..."
    docker-compose logs certbot | tail -20
    exit 1
fi

echo ""
echo "📊 Container status:"
docker-compose ps
