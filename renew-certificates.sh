#!/bin/bash

echo "?? EMPIRE TCG SSL CERTIFICATE RENEWAL"
echo "====================================="

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
if [ ! -f "certbot/conf/live/empirecardgame.com/fullchain.pem" ]; then
    print_error "No certificates found to renew"
    print_status "Run: ./deploy-https-certificates.sh to generate initial certificates"
    exit 1
fi

# Test renewal (dry run)
print_status "Testing certificate renewal (dry run)..."
docker-compose -f docker-compose-cms.yml run --rm certbot renew --dry-run

if [ $? -eq 0 ]; then
    print_success "Renewal test passed"
    
    print_status "Performing actual certificate renewal..."
    docker-compose -f docker-compose-cms.yml run --rm certbot renew --verbose
    
    if [ $? -eq 0 ]; then
        print_success "? Certificates renewed successfully"
        
        print_status "Reloading nginx with new certificates..."
        docker-compose -f docker-compose-cms.yml restart nginx
        
        # Wait for nginx to reload
        sleep 10
        
        # Test HTTPS after renewal
        print_status "Testing HTTPS after renewal..."
        HTTPS_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://empirecardgame.com 2>/dev/null || echo "000")
        
        if [ "$HTTPS_STATUS" = "200" ] || [ "$HTTPS_STATUS" = "302" ] || [ "$HTTPS_STATUS" = "301" ]; then
            print_success "?? HTTPS working after renewal (status: $HTTPS_STATUS)"
            
            # Show new expiry date
            print_status "New certificate expiry:"
            EXPIRY=$(openssl x509 -enddate -noout -in certbot/conf/live/empirecardgame.com/fullchain.pem 2>/dev/null | cut -d= -f2)
            if [ ! -z "$EXPIRY" ]; then
                print_success "New expiry date: $EXPIRY"
            fi
            
        else
            print_error "HTTPS test failed after renewal (status: $HTTPS_STATUS)"
            print_error "Check nginx logs: docker-compose -f docker-compose-cms.yml logs nginx"
        fi
        
    else
        print_error "Certificate renewal failed"
        print_status "Check certbot logs: docker-compose -f docker-compose-cms.yml logs certbot"
        exit 1
    fi
    
else
    print_error "Renewal test failed"
    print_error "Cannot proceed with actual renewal"
    print_status "Check certbot logs: docker-compose -f docker-compose-cms.yml logs certbot"
    exit 1
fi

echo ""
print_success "?? Certificate renewal complete!"
echo ""
print_status "?? To automate renewal, add this to crontab (crontab -e):"
echo "0 2 * * 1 /path/to/$(pwd)/renew-certificates.sh >> /var/log/ssl-renewal.log 2>&1"