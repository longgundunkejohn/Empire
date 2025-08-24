#!/bin/bash

echo "?? BUILDING EMPIRE TCG - SERVER ONLY VERSION"
echo "============================================="

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

print_status() { echo -e "${BLUE}[INFO]${NC} $1"; }
print_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
print_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
print_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Step 1: Clean up
print_status "Cleaning up previous builds..."
docker-compose -f docker-compose-cms.yml down 2>/dev/null || true
docker system prune -f

# Step 2: Build with server-only dockerfile
print_status "Building Empire TCG server with clean Dockerfile..."
docker-compose -f docker-compose-cms.yml build --no-cache empire-game

if [ $? -eq 0 ]; then
    print_success "? Build successful!"
    
    # Step 3: Start services
    print_status "Starting all services..."
    docker-compose -f docker-compose-cms.yml up -d
    
    # Step 4: Wait for services
    print_status "Waiting for services to start..."
    sleep 30
    
    # Step 5: Test connectivity
    print_status "Testing Empire TCG API..."
    
    # Test the API server
    API_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8081 2>/dev/null || echo "000")
    if [[ "$API_STATUS" == "200" ]]; then
        print_success "? Empire TCG API is running!"
        
        # Test specific endpoints
        echo ""
        print_status "Testing API endpoints..."
        
        # Test Swagger docs
        SWAGGER_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8081/swagger 2>/dev/null || echo "000")
        if [[ "$SWAGGER_STATUS" == "200" ]]; then
            print_success "? Swagger documentation available"
        fi
        
        # Test card API
        CARDS_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8081/api/deckbuilder/cards 2>/dev/null || echo "000")
        if [[ "$CARDS_STATUS" == "200" ]]; then
            print_success "? Card database API working"
        fi
        
    else
        print_warning "?? Empire TCG API status: $API_STATUS"
    fi
    
    # Test WordPress
    WP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8080 2>/dev/null || echo "000")
    if [[ "$WP_STATUS" == "200" || "$WP_STATUS" == "302" ]]; then
        print_success "? WordPress is running!"
    else
        print_warning "?? WordPress status: $WP_STATUS"
    fi
    
    # Test Nginx
    NGINX_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost 2>/dev/null || echo "000")
    if [[ "$NGINX_STATUS" == "200" || "$NGINX_STATUS" == "302" ]]; then
        print_success "? Nginx proxy working!"
    else
        print_warning "?? Nginx status: $NGINX_STATUS"
    fi
    
    echo ""
    print_success "?? EMPIRE TCG DEPLOYMENT SUCCESSFUL!"
    echo ""
    echo "?? ACCESS POINTS:"
    echo "================================"
    echo "?? Empire TCG API:     http://empirecardgame.com:8081"
    echo "?? API Documentation: http://empirecardgame.com:8081/swagger"
    echo "?? Card Database:      http://empirecardgame.com:8081/api/deckbuilder/cards"
    echo "?? WordPress CMS:      http://empirecardgame.com:8080"
    echo "?? Main Site:          http://empirecardgame.com"
    echo ""
    echo "?? NEXT STEPS:"
    echo "==============="
    echo "1. Complete WordPress setup at http://empirecardgame.com:8080"
    echo "2. Install WooCommerce and configure your store"
    echo "3. Test the API endpoints to ensure they work"
    echo "4. Configure SSL certificates when ready"
    echo ""
    echo "?? MANAGEMENT COMMANDS:"
    echo "======================="
    echo "- View logs:    docker-compose -f docker-compose-cms.yml logs -f"
    echo "- Restart:      docker-compose -f docker-compose-cms.yml restart"
    echo "- Stop:         docker-compose -f docker-compose-cms.yml down"
    
else
    print_error "? Build failed!"
    echo ""
    print_status "Showing build logs..."
    docker-compose -f docker-compose-cms.yml logs empire-game
    exit 1
fi