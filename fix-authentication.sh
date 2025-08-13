#!/bin/bash
echo "?? Empire TCG Authentication Fix Deployment Script"
echo "=================================================="

# Stop the current containers
echo "?? Stopping current containers..."
docker-compose down

# Backup current database
echo "?? Backing up database..."
if [ -f "empire.db" ]; then
    cp empire.db "empire.db.backup.$(date +%Y%m%d_%H%M%S)"
    echo "? Database backed up"
fi

# Build new images without cache
echo "??? Building new images..."
docker-compose build --no-cache

# Start services
echo "?? Starting services..."
docker-compose up -d

# Wait for services to be ready
echo "? Waiting for services to start..."
sleep 30

# Check if containers are running
echo "?? Checking container status..."
docker-compose ps

# Check application logs
echo "?? Checking application logs..."
docker-compose logs --tail=50 empire-tcg

# Test the authentication endpoints
echo "?? Testing authentication endpoints..."
echo "Testing registration endpoint..."
curl -X POST https://empirecardgame.com/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser123","password":"testpassword123","confirmPassword":"testpassword123"}' \
  -w "\nHTTP Status: %{http_code}\n" \
  -s -o /tmp/register_test.json

echo "Registration response:"
cat /tmp/register_test.json
echo ""

echo "Testing login endpoint..."
curl -X POST https://empirecardgame.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser123","password":"testpassword123"}' \
  -w "\nHTTP Status: %{http_code}\n" \
  -s -o /tmp/login_test.json

echo "Login response:"
cat /tmp/login_test.json
echo ""

# Check SSL certificate
echo "?? Checking SSL certificate..."
echo | openssl s_client -servername empirecardgame.com -connect empirecardgame.com:443 2>/dev/null | openssl x509 -noout -dates

# Clean up test files
rm -f /tmp/register_test.json /tmp/login_test.json

echo "?? Deployment complete!"
echo ""
echo "?? Next steps:"
echo "1. Test registration at https://empirecardgame.com/register"
echo "2. Test login at https://empirecardgame.com/login"
echo "3. Check browser console for any errors"
echo "4. Verify JWT tokens are being stored in localStorage"
echo ""
echo "?? Monitoring commands:"
echo "- docker-compose logs -f empire-tcg    # View live logs"
echo "- docker-compose ps                    # Check container status"
echo "- docker-compose restart empire-tcg    # Restart if needed"