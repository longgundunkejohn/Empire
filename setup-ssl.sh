#!/bin/bash

echo "🔐 Setting up SSL certificates for Empire TCG..."

# Stop existing containers
echo "🛑 Stopping existing containers..."
docker-compose down

# Create SSL directory
echo "📁 Creating SSL directory..."
mkdir -p nginx/ssl

# Start containers without SSL first (HTTP only)
echo "🚀 Starting containers for certificate generation..."
docker-compose up -d empire-app nginx

# Wait for nginx to be ready
echo "⏳ Waiting for nginx to be ready..."
sleep 10

# Generate SSL certificates
echo "🔒 Generating SSL certificates..."
docker-compose run --rm certbot

# Check if certificates were generated
if [ -f "nginx/ssl/live/empirecardgame.com/fullchain.pem" ]; then
    echo "✅ SSL certificates generated successfully!"
    
    # Restart nginx with SSL
    echo "🔄 Restarting nginx with SSL..."
    docker-compose restart nginx
    
    echo "🎉 SSL setup complete!"
    echo "🌐 Your site should now be accessible at:"
    echo "   https://empirecardgame.com"
    echo "   https://www.empirecardgame.com"
else
    echo "❌ SSL certificate generation failed!"
    echo "Please check the logs: docker-compose logs certbot"
    exit 1
fi

# Show status
echo "📊 Container status:"
docker-compose ps
