#!/bin/bash

echo "🔧 Final SSL Fix for Empire TCG"
echo "==============================="

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

# Stop containers
print_status "Stopping all containers..."
docker-compose down
docker system prune -f

# Create the correct nginx config that will actually work
print_status "Creating working nginx config..."
cat > nginx/nginx-working.conf << 'EOF'
events {
    worker_connections 1024;
}

http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;

    # Basic settings
    sendfile on;
    tcp_nopush on;
    tcp_nodelay on;
    keepalive_timeout 65;
    client_max_body_size 16M;

    # Upstream for the .NET application
    upstream empire_app {
        server empire-tcg:8080;
    }

    # HTTP server for SSL setup
    server {
        listen 80;
        server_name empirecardgame.com www.empirecardgame.com;

        # Let's Encrypt challenge directory - serve files directly from the volume
        location /.well-known/acme-challenge/ {
            root /var/www/certbot;
            try_files $uri =404;
            add_header Content-Type text/plain;
        }

        # Everything else goes to the app
        location / {
            proxy_pass http://empire_app;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }
    }
}
EOF

# Create a temporary docker-compose that uses the working config
print_status "Creating temporary docker-compose..."
cat > docker-compose-ssl.yml << 'EOF'
version: '3.8'

services:
  empire-app:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: empire-tcg
    restart: unless-stopped
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
    volumes:
      - empire-data:/app/data
    networks:
      - empire-network

  nginx:
    image: nginx:alpine
    container_name: empire-nginx
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx-working.conf:/etc/nginx/nginx.conf:ro
      - ./nginx/ssl:/etc/nginx/ssl:ro
      - certbot-data:/var/www/certbot
    networks:
      - empire-network
    depends_on:
      - empire-app

  certbot:
    image: certbot/certbot
    container_name: empire-certbot
    volumes:
      - ./nginx/ssl:/etc/letsencrypt
      - certbot-data:/var/www/certbot
    command: certonly --webroot --webroot-path=/var/www/certbot --email oskarmelorm@gmail.com --agree-tos --no-eff-email -d empirecardgame.com -d www.empirecardgame.com
    networks:
      - empire-network

volumes:
  empire-data:
    driver: local
  certbot-data:
    driver: local

networks:
  empire-network:
    driver: bridge
EOF

# Start containers with the working config
print_status "Starting containers with working SSL config..."
docker-compose -f docker-compose-ssl.yml up -d

# Wait for containers to start
print_status "Waiting for containers to start..."
sleep 15

# Test HTTP access
print_status "Testing HTTP access..."
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://empirecardgame.com || echo "000")
if [[ "$HTTP_STATUS" == "200" ]]; then
    print_success "HTTP access working!"
else
    print_error "HTTP access failed (Status: $HTTP_STATUS)"
    docker-compose -f docker-compose-ssl.yml logs nginx
    exit 1
fi

# Create a test challenge file directly in the volume
print_status "Creating test challenge file in the correct location..."
docker exec empire-nginx mkdir -p /var/www/certbot/.well-known/acme-challenge/
docker exec empire-nginx sh -c 'echo "test-challenge-content" > /var/www/certbot/.well-known/acme-challenge/test-file'

# Test challenge file access
print_status "Testing challenge file access..."
CHALLENGE_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://empirecardgame.com/.well-known/acme-challenge/test-file || echo "000")
if [[ "$CHALLENGE_STATUS" == "200" ]]; then
    print_success "Challenge file access working!"
    
    # Clean up test file
    docker exec empire-nginx rm -f /var/www/certbot/.well-known/acme-challenge/test-file
    
    # Now try to get SSL certificates
    print_status "Attempting SSL certificate generation..."
    if docker-compose -f docker-compose-ssl.yml run --rm certbot; then
        print_success "SSL certificates generated successfully!"
        
        # Stop the temporary setup
        docker-compose -f docker-compose-ssl.yml down
        
        # Start with the full HTTPS configuration
        print_status "Starting with full HTTPS configuration..."
        docker-compose up -d
        
        # Test HTTPS
        sleep 15
        for domain in "empirecardgame.com" "www.empirecardgame.com"; do
            HTTPS_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://$domain || echo "000")
            if [[ "$HTTPS_STATUS" == "200" ]]; then
                print_success "HTTPS working for $domain!"
            else
                print_warning "HTTPS may need a moment for $domain (Status: $HTTPS_STATUS)"
            fi
        done
        
        print_success "🎉 SSL setup completed successfully!"
        echo ""
        echo "Your Empire TCG site is now accessible at:"
        echo "  https://empirecardgame.com"
        echo "  https://www.empirecardgame.com"
        echo ""
        echo "Cleaning up temporary files..."
        rm -f docker-compose-ssl.yml nginx/nginx-working.conf
        
    else
        print_error "SSL certificate generation failed!"
        docker-compose -f docker-compose-ssl.yml logs certbot
        exit 1
    fi
    
else
    print_error "Challenge file access failed (Status: $CHALLENGE_STATUS)"
    print_status "Checking nginx configuration..."
    docker exec empire-nginx cat /etc/nginx/nginx.conf
    print_status "Checking nginx logs..."
    docker-compose -f docker-compose-ssl.yml logs nginx | tail -20
    exit 1
fi
