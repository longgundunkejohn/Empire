#!/bin/bash

echo "🔧 Empire TCG Complete Deployment Fix"
echo "====================================="

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

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Stop all containers
print_status "Stopping all containers..."
docker-compose down
docker stop $(docker ps -aq) 2>/dev/null || true

# Clean up Docker system
print_status "Cleaning Docker system..."
docker system prune -f

# Check if nginx config exists and has correct content
print_status "Verifying nginx configuration..."
if grep -q "empire-tcg:8080" nginx/default.conf; then
    print_success "Nginx configuration already updated"
else
    print_status "Updating nginx configuration..."
    sed -i 's/empire-server:80/empire-tcg:8080/g' nginx/default.conf
    print_success "Nginx configuration updated"
fi

# Start with the correct docker-compose
print_status "Building and starting containers..."
docker-compose up -d --build

# Wait for containers to start
print_status "Waiting for containers to start..."
sleep 30

# Check container status
print_status "Checking container status..."
docker-compose ps

# Check if empire-tcg container is running
if docker ps --format "table {{.Names}}" | grep -q "empire-tcg"; then
    print_success "Empire TCG container is running"
else
    print_error "Empire TCG container is not running"
    print_status "Checking container logs..."
    docker-compose logs empire-app
    exit 1
fi

# Check if nginx container is running
if docker ps --format "table {{.Names}}" | grep -q "empire-nginx"; then
    print_success "Nginx container is running"
else
    print_error "Nginx container is not running"
    print_status "Checking nginx logs..."
    docker-compose logs nginx
    exit 1
fi

# Test internal connectivity
print_status "Testing internal container connectivity..."
if docker exec empire-nginx wget -q --spider http://empire-tcg:8080; then
    print_success "Internal connectivity working"
else
    print_error "Internal connectivity failed"
    print_status "Checking network configuration..."
    docker network ls
    docker network inspect $(docker-compose ps -q | head -1 | xargs docker inspect --format='{{range .NetworkSettings.Networks}}{{.NetworkID}}{{end}}' | cut -c1-12)
fi

# Test HTTP access
print_status "Testing HTTP access..."
sleep 5
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://empirecardgame.com || echo "000")
if [[ "$HTTP_STATUS" == "200" ]]; then
    print_success "HTTP access working!"
else
    print_error "HTTP access failed (Status: $HTTP_STATUS)"
    print_status "Checking nginx error logs..."
    docker-compose logs nginx | tail -20
    print_status "Checking app logs..."
    docker-compose logs empire-app | tail -20
    
    # Try to get more detailed curl output
    print_status "Detailed HTTP test..."
    curl -v http://empirecardgame.com 2>&1 | head -20
    exit 1
fi

# Test HTTPS access
print_status "Testing HTTPS access..."
HTTPS_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://empirecardgame.com || echo "000")
if [[ "$HTTPS_STATUS" == "200" ]]; then
    print_success "HTTPS access working!"
else
    print_warning "HTTPS access needs attention (Status: $HTTPS_STATUS)"
    
    # Check if SSL certificates exist
    if docker exec empire-nginx ls -la /etc/letsencrypt/live/empirecardgame.com/ 2>/dev/null; then
        print_status "SSL certificates found, reloading nginx..."
        docker-compose exec nginx nginx -s reload
        sleep 10
        
        # Test HTTPS again
        HTTPS_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://empirecardgame.com || echo "000")
        if [[ "$HTTPS_STATUS" == "200" ]]; then
            print_success "HTTPS working after reload!"
        else
            print_error "HTTPS still not working after reload"
            print_status "Checking SSL certificate validity..."
            docker exec empire-nginx openssl x509 -in /etc/letsencrypt/live/empirecardgame.com/fullchain.pem -text -noout 2>/dev/null || print_warning "Cannot check SSL certificate"
        fi
    else
        print_warning "SSL certificates not found - HTTPS will not work"
        print_status "To generate SSL certificates, run: docker-compose run --rm certbot"
    fi
fi

# Test specific lobby endpoint
print_status "Testing lobby endpoint..."
LOBBY_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://empirecardgame.com/lobby || echo "000")
if [[ "$LOBBY_STATUS" == "200" ]]; then
    print_success "Lobby endpoint working!"
else
    print_warning "Lobby endpoint status: $LOBBY_STATUS"
fi

# Final status summary
echo ""
print_success "🎉 Empire TCG deployment fix completed!"
echo ""
echo "📊 Status Summary:"
echo "  HTTP:  $(curl -s -o /dev/null -w "%{http_code}" http://empirecardgame.com || echo "000")"
echo "  HTTPS: $(curl -s -o /dev/null -w "%{http_code}" https://empirecardgame.com || echo "000")"
echo "  Lobby: $(curl -s -o /dev/null -w "%{http_code}" https://empirecardgame.com/lobby || echo "000")"
echo ""
echo "🌐 Your site should now be accessible at:"
echo "  https://empirecardgame.com"
echo "  https://www.empirecardgame.com"
echo "  https://empirecardgame.com/lobby"
echo ""
echo "📋 Useful commands:"
echo "  View logs: docker-compose logs -f"
echo "  Restart:   docker-compose restart"
echo "  Stop:      docker-compose down"     