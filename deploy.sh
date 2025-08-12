#!/bin/bash

# Empire TCG Deployment Script
set -e

echo "🚀 Starting Empire TCG deployment..."

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker and try again."
    exit 1
fi

# Check if docker-compose is available
if ! command -v docker-compose &> /dev/null; then
    echo "❌ docker-compose is not installed. Please install it and try again."
    exit 1
fi

# Stop existing containers
echo "🛑 Stopping existing containers..."
docker-compose down --remove-orphans

# Remove old images to ensure fresh build
echo "🧹 Cleaning up old images..."
docker image prune -f
docker-compose build --no-cache

# Create SSL directory
echo "📁 Creating SSL directory..."
mkdir -p nginx/ssl

# Start the application
echo "🏗️ Starting Empire TCG..."
docker-compose up -d

# Wait for services to be ready
echo "⏳ Waiting for services to start..."
sleep 10

# Check if services are running
if docker-compose ps | grep -q "Up"; then
    echo "✅ Empire TCG is now running!"
    echo ""
    echo "🌐 Access the application at:"
    echo "   HTTP:  http://localhost"
    echo "   HTTPS: https://localhost (after SSL setup)"
    echo ""
    echo "📋 To view logs: docker-compose logs -f"
    echo "🛑 To stop: docker-compose down"
    echo ""
    echo "⚠️  Note: For production deployment:"
    echo "   1. Update nginx/nginx.conf with your domain name"
    echo "   2. Update docker-compose.yml with your email and domain"
    echo "   3. Run: docker-compose run --rm certbot"
else
    echo "❌ Failed to start services. Check logs with: docker-compose logs"
    exit 1
fi
