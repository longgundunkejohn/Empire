#!/bin/bash

echo "🔍 Empire TCG Diagnostic and Fix"
echo "================================"

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

# Check current container status
print_status "Checking current container status..."
docker ps -a

print_status "Checking docker-compose status..."
docker-compose ps

# Check if containers are running
NGINX_RUNNING=$(docker ps --filter "name=empire-nginx" --filter "status=running" -q)
APP_RUNNING=$(docker ps --filter "name=empire-tcg" --filter "status=running" -q)

if [[ -z "$NGINX_RUNNING" ]]; then
    print_error "Nginx container is not running"
else
    print_success "Nginx container is running"
fi

if [[ -z "$APP_RUNNING" ]]; then
    print_error "App container is not running"
else
    print_success "App container is running"
fi

# Check SSL certificates
print_status "Checking SSL certificates..."
if [[ -d "nginx/ssl/live/empirecardgame.com" ]]; then
    print_success "SSL certificates exist"
    ls -la nginx/ssl/live/empirecardgame.com/
else
    print_error "SSL certificates not found"
fi

# Check nginx configuration
print_status "Checking nginx configuration..."
if [[ -f "nginx/nginx.conf" ]]; then
    print_success "Main nginx config exists"
else
    print_error "Main nginx config missing"
fi

# Try to restart everything
print_status "Attempting to restart all services..."
docker-compose down
sleep 5
docker-compose up -d

print_status "Waiting for services to start..."
sleep 15

# Test connectivity
print_status "Testing HTTP connectivity..."
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://empirecardgame.com || echo "000")
print_status "HTTP Status: $HTTP_STATUS"

print_status "Testing HTTPS connectivity..."
HTTPS_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://empirecardgame.com || echo "000")
print_status "HTTPS Status: $HTTPS_STATUS"

# Check logs if there are issues
if [[ "$HTTPS_STATUS" != "200" ]]; then
    print_warning "HTTPS not working, checking logs..."
    print_status "Nginx logs:"
    docker-compose logs nginx | tail -20
    print_status "App logs:"
    docker-compose logs empire-app | tail -20
fi

# Final status
print_status "Final container status:"
docker-compose ps

if [[ "$HTTPS_STATUS" == "200" ]]; then
    print_success "🎉 Site is working! Visit https://empirecardgame.com"
else
    print_error "Site still not working. Check the logs above for issues."
fi
