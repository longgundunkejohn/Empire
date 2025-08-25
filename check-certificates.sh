#!/bin/bash

echo "?? EMPIRE TCG SSL CERTIFICATE STATUS"
echo "===================================="

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

# Check if certificates exist
if [ -f "certbot/conf/live/empirecardgame.com/fullchain.pem" ]; then
    print_success "SSL certificates found"
    
    # Show certificate details
    print_status "Certificate information:"
    docker-compose -f docker-compose-cms.yml run --rm certbot certificates
    
    # Check expiry
    print_status "Certificate expiry check:"
    EXPIRY=$(openssl x509 -enddate -noout -in certbot/conf/live/empirecardgame.com/fullchain.pem 2>/dev/null | cut -d= -f2)
    if [ ! -z "$EXPIRY" ]; then
        print_status "Expires: $EXPIRY"
        
        # Calculate days until expiry
        EXPIRY_EPOCH=$(date -d "$EXPIRY" +%s 2>/dev/null)
        CURRENT_EPOCH=$(date +%s)
        DAYS_LEFT=$(( (EXPIRY_EPOCH - CURRENT_EPOCH) / 86400 ))
        
        if [ $DAYS_LEFT -gt 30 ]; then
            print_success "Certificate valid for $DAYS_LEFT more days"
        elif [ $DAYS_LEFT -gt 7 ]; then
            print_warning "Certificate expires in $DAYS_LEFT days - consider renewing soon"
        else
            print_error "Certificate expires in $DAYS_LEFT days - URGENT RENEWAL NEEDED"
        fi
    fi
    
    # Test HTTPS connectivity
    print_status "Testing HTTPS connectivity..."
    HTTPS_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://empirecardgame.com 2>/dev/null || echo "000")
    
    if [ "$HTTPS_STATUS" = "200" ] || [ "$HTTPS_STATUS" = "302" ] || [ "$HTTPS_STATUS" = "301" ]; then
        print_success "HTTPS working (status: $HTTPS_STATUS)"
    else
        print_error "HTTPS not working (status: $HTTPS_STATUS)"
    fi
    
    # Test SSL certificate validity
    print_status "Testing SSL certificate validity..."
    SSL_CHECK=$(echo | openssl s_client -servername empirecardgame.com -connect empirecardgame.com:443 2>/dev/null | openssl x509 -noout -dates 2>/dev/null)
    
    if [ $? -eq 0 ]; then
        print_success "SSL certificate valid and accessible"
    else
        print_warning "SSL certificate check failed"
    fi
    
else
    print_error "No SSL certificates found"
    print_status "Run: ./deploy-https-certificates.sh to generate certificates"
fi

echo ""
print_status "?? Available commands:"
echo "?? Check status: ./check-certificates.sh"
echo "?? Renew certificates: ./renew-certificates.sh"
echo "?? Generate new: ./deploy-https-certificates.sh"