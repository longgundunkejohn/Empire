#!/bin/bash

echo "?? Empire TCG Deployment Diagnostics"
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

# Check Docker
print_status "Checking Docker status..."
if docker --version &>/dev/null; then
    print_success "Docker is installed: $(docker --version)"
else
    print_error "Docker is not installed or not running"
    exit 1
fi

# Check Docker Compose
print_status "Checking Docker Compose..."
if docker-compose --version &>/dev/null; then
    print_success "Docker Compose is available: $(docker-compose --version)"
else
    print_error "Docker Compose is not available"
    exit 1
fi

# Check container status
print_status "Checking container status..."
echo "Running containers:"
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

echo ""
echo "All containers (including stopped):"
docker ps -a --format "table {{.Names}}\t{{.Status}}\t{{.Image}}"

# Check docker-compose services
print_status "Checking docker-compose services..."
if [ -f "docker-compose.yml" ]; then
    docker-compose ps
else
    print_error "docker-compose.yml not found"
fi

# Check nginx configuration
print_status "Checking nginx configuration..."
if [ -f "nginx/default.conf" ]; then
    if grep -q "empire-tcg:8080" nginx/default.conf; then
        print_success "Nginx config has correct container reference"
    else
        print_error "Nginx config still references wrong container"
        echo "Current proxy_pass lines:"
        grep "proxy_pass" nginx/default.conf || echo "No proxy_pass found"
    fi
else
    print_error "nginx/default.conf not found"
fi

# Check SSL certificates
print_status "Checking SSL certificates..."
if [ -d "nginx/ssl/live/empirecardgame.com" ]; then
    print_success "SSL certificate directory exists"
    ls -la nginx/ssl/live/empirecardgame.com/
else
    print_warning "SSL certificates not found"
fi

# Test network connectivity
print_status "Testing network connectivity..."

# Test HTTP
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://empirecardgame.com 2>/dev/null || echo "000")
echo "HTTP Status: $HTTP_STATUS"

# Test HTTPS
HTTPS_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://empirecardgame.com 2>/dev/null || echo "000")
echo "HTTPS Status: $HTTPS_STATUS"

# Test DNS
print_status "Checking DNS resolution..."
DOMAIN_IP=$(dig +short empirecardgame.com 2>/dev/null || echo "N/A")
SERVER_IP=$(curl -s ifconfig.me 2>/dev/null || echo "N/A")
echo "Domain resolves to: $DOMAIN_IP"
echo "Server IP: $SERVER_IP"

if [ "$DOMAIN_IP" = "$SERVER_IP" ]; then
    print_success "DNS is correctly configured"
else
    print_warning "DNS may not be pointing to this server"
fi

# Check logs if containers are running
print_status "Recent container logs..."
if docker ps --format "{{.Names}}" | grep -q "empire-nginx"; then
    echo "=== Nginx Logs (last 10 lines) ==="
    docker logs empire-nginx --tail 10
fi

if docker ps --format "{{.Names}}" | grep -q "empire-tcg"; then
    echo "=== Empire App Logs (last 10 lines) ==="
    docker logs empire-tcg --tail 10
fi

# Check disk space
print_status "Checking disk space..."
df -h

echo ""
print_status "Diagnostic complete. If issues persist, check the detailed logs above."