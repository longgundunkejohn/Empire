#!/bin/bash

echo "🚨 Emergency SSL Fix for Empire TCG"
echo "=================================="

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
print_status "Stopping containers..."
docker-compose down

# Create a simple nginx config that serves challenge files correctly
print_status "Creating emergency nginx config..."
cat > nginx/nginx-emergency.conf << 'EOF'
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

        # Let's Encrypt challenge directory - serve files directly
        location /.well-known/acme-challenge/ {
            root /var/www/certbot;
            try_files $uri =404;
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

# Update docker-compose to use emergency config
print_status "Updating docker-compose to use emergency config..."
cp docker-compose.yml docker-compose.yml.backup
sed 's|./nginx/nginx.conf|./nginx/nginx-emergency.conf|g' docker-compose.yml > docker-compose-temp.yml
mv docker-compose-temp.yml docker-compose.yml

# Start containers
print_status "Starting containers with emergency config..."
docker-compose up -d

# Wait for containers to start
print_status "Waiting for containers to start..."
sleep 10

# Test HTTP access
print_status "Testing HTTP access..."
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://empirecardgame.com || echo "000")
if [[ "$HTTP_STATUS" == "200" ]]; then
    print_success "HTTP access working!"
else
    print_error "HTTP access failed (Status: $HTTP_STATUS)"
    exit 1
fi

# Create a test challenge file to verify the setup
print_status "Creating test challenge file..."
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
    if docker-compose run --rm certbot; then
        print_success "SSL certificates generated successfully!"
        
        # Restore original docker-compose and switch to HTTPS
        print_status "Switching to HTTPS configuration..."
        mv docker-compose.yml.backup docker-compose.yml
        docker-compose restart nginx
        
        # Test HTTPS
        sleep 10
        for domain in "empirecardgame.com" "www.empirecardgame.com"; do
            HTTPS_STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://$domain || echo "000")
            if [[ "$HTTPS_STATUS" == "200" ]]; then
                print_success "HTTPS working for $domain!"
            else
                print_warning "HTTPS may need a moment for $domain (Status: $HTTPS_STATUS)"
            fi
        done
        
        print_success "🎉 SSL setup completed successfully!"
        echo "Your site should now be accessible at:"
        echo "  https://empirecardgame.com"
        echo "  https://www.empirecardgame.com"
        
    else
        print_error "SSL certificate generation failed!"
        docker-compose logs certbot
        exit 1
    fi
    
else
    print_error "Challenge file access failed (Status: $CHALLENGE_STATUS)"
    print_status "Checking nginx logs..."
    docker-compose logs nginx | tail -20
    exit 1
fi
