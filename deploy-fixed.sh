#!/bin/bash

echo "?? FIXED DOCKER DEPLOYMENT FOR EMPIRE TCG"
echo "========================================"
echo "Deploying with WebAssembly build fixes"
echo ""

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

print_status() { echo -e "${BLUE}[INFO]${NC} $1"; }
print_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
print_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
print_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Step 1: Clean up any existing containers
print_status "Cleaning up existing containers..."
docker-compose -f docker-compose-cms.yml down 2>/dev/null || true
docker system prune -f

# Step 2: Create necessary directories
print_status "Creating directory structure..."
mkdir -p {wordpress,mysql-data,game-data,game-logs,certbot/{conf,www}}
mkdir -p nginx/ssl
chmod -R 755 wordpress mysql-data game-data certbot

# Step 3: Build with better error handling
print_status "Building Empire TCG with WebAssembly fixes..."

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    print_error "Docker is not running. Please start Docker first."
    exit 1
fi

# Start the build
print_status "Starting container build..."
docker-compose -f docker-compose-cms.yml build --no-cache empire-game

# Check if build succeeded
if [ $? -eq 0 ]; then
    print_success "? Empire TCG game container built successfully!"
else
    print_error "? Game container build failed"
    echo ""
    echo "Common fixes:"
    echo "1. Check Docker has enough memory (at least 4GB)"
    echo "2. Try: docker system prune -a"
    echo "3. Check internet connection for NuGet packages"
    exit 1
fi

# Step 4: Build other containers
print_status "Building other containers..."
docker-compose -f docker-compose-cms.yml build

# Step 5: Start all services
print_status "Starting all services..."
docker-compose -f docker-compose-cms.yml up -d

# Step 6: Wait and check status
print_status "Waiting for services to start..."
sleep 30

print_status "Checking container status..."
docker-compose -f docker-compose-cms.yml ps

# Step 7: Test connectivity
print_status "Testing connectivity..."

# Test WordPress
WP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8080 2>/dev/null || echo "000")
if [[ "$WP_STATUS" == "200" || "$WP_STATUS" == "302" ]]; then
    print_success "? WordPress accessible on port 8080"
else
    print_warning "?? WordPress status: $WP_STATUS"
fi

# Test Game
GAME_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8081 2>/dev/null || echo "000")
if [[ "$GAME_STATUS" == "200" ]]; then
    print_success "? Game accessible on port 8081"
else
    print_warning "?? Game status: $GAME_STATUS"
fi

# Test Nginx
NGINX_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost 2>/dev/null || echo "000")
if [[ "$NGINX_STATUS" == "200" || "$NGINX_STATUS" == "302" ]]; then
    print_success "? Nginx proxy working on port 80"
else
    print_warning "?? Nginx status: $NGINX_STATUS"
fi

echo ""
print_success "?? DEPLOYMENT COMPLETE!"
echo ""
echo "?? ACCESS YOUR PLATFORM:"
echo "========================"
echo "WordPress: http://localhost:8080 (direct)"
echo "Game: http://localhost:8081 (direct)"  
echo "Main Site: http://localhost (via nginx)"
echo ""
echo "?? NEXT STEPS:"
echo "=============="
echo "1. Complete WordPress setup at http://localhost:8080"
echo "2. Database connection details:"
echo "   - Host: mysql"
echo "   - Database: empire_wordpress"
echo "   - Username: empire_user"
echo "   - Password: empire_secure_2024"
echo ""
echo "3. Install plugins:"
echo "   - WooCommerce"
echo "   - WooCommerce Stripe Gateway"
echo "   - Elementor"
echo ""
echo "4. Configure Stripe:"
echo "   - Get API keys from dashboard.stripe.com"
echo "   - Add to WooCommerce settings"
echo ""

# Show logs if there are issues
if [[ "$WP_STATUS" != "200" && "$WP_STATUS" != "302" ]] || [[ "$GAME_STATUS" != "200" ]]; then
    echo ""
    print_warning "?? Some services may need attention. Checking logs..."
    echo ""
    echo "WordPress logs:"
    docker-compose -f docker-compose-cms.yml logs --tail=10 wordpress
    echo ""
    echo "Game logs:"  
    docker-compose -f docker-compose-cms.yml logs --tail=10 empire-game
fi

print_status "?? Deployment script completed!"
echo ""
echo "?? Use these commands for management:"
echo "- View logs: docker-compose -f docker-compose-cms.yml logs -f"
echo "- Restart: docker-compose -f docker-compose-cms.yml restart"
echo "- Stop: docker-compose -f docker-compose-cms.yml down"