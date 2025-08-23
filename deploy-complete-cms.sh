#!/bin/bash

echo "?? Empire TCG Complete Platform Deployment"
echo "=========================================="
echo "WordPress CMS + WooCommerce + Stripe + Blazor Game Integration"
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

print_status() { echo -e "${BLUE}[INFO]${NC} $1"; }
print_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
print_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
print_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Check if we're in the right directory
if [ ! -f "docker-compose-cms.yml" ]; then
    print_error "docker-compose-cms.yml not found. Please run this script from the project root."
    exit 1
fi

# Step 1: Stop existing containers
print_status "Stopping existing containers..."
docker-compose down 2>/dev/null || true
docker-compose -f docker-compose-cms.yml down 2>/dev/null || true

# Step 2: Create necessary directories
print_status "Creating directory structure..."
mkdir -p {wordpress,mysql-data,game-data,game-logs,certbot/{conf,www}}
mkdir -p wordpress/wp-content/{themes/empire-tcg,plugins/empire-integration,uploads}
mkdir -p nginx/ssl

# Step 3: Set proper permissions
print_status "Setting directory permissions..."
chmod -R 755 wordpress
chmod -R 755 game-data
chmod -R 755 certbot

# Step 4: Backup existing data if present
if [ -f "empire.db" ]; then
    print_status "Backing up existing game database..."
    cp empire.db "empire.db.backup.$(date +%Y%m%d_%H%M%S)"
fi

# Step 5: Build and start containers
print_status "Building and starting containers..."
docker-compose -f docker-compose-cms.yml up -d --build

# Step 6: Wait for services to be ready
print_status "Waiting for services to start..."
sleep 30

# Step 7: Check container status
print_status "Checking container status..."
docker-compose -f docker-compose-cms.yml ps

# Step 8: Test WordPress access
print_status "Testing WordPress access..."
sleep 10

# Try HTTP first
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8080 || echo "000")
if [[ "$HTTP_STATUS" == "200" || "$HTTP_STATUS" == "302" ]]; then
    print_success "WordPress is accessible on HTTP"
else
    print_warning "WordPress HTTP status: $HTTP_STATUS"
fi

# Test game access
GAME_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost/play/ || echo "000")
if [[ "$GAME_STATUS" == "200" ]]; then
    print_success "Game is accessible"
else
    print_warning "Game access status: $GAME_STATUS"
fi

# Step 9: Initialize WordPress if needed
print_status "WordPress setup instructions:"
echo ""
echo "?? WordPress Setup:"
echo "1. Go to: http://empirecardgame.com (or your domain)"
echo "2. Complete the WordPress installation wizard"
echo "3. Database details:"
echo "   - Database Name: empire_wordpress"
echo "   - Username: empire_user"
echo "   - Password: empire_secure_2024"
echo "   - Database Host: mysql"
echo ""

# Step 10: Plugin and theme setup
print_status "Setting up Empire TCG theme and plugin..."
echo "4. After WordPress setup:"
echo "   - Go to Appearance > Themes"
echo "   - Activate 'Empire TCG' theme"
echo "   - Go to Plugins"
echo "   - Activate 'Empire TCG Integration' plugin"
echo ""

# Step 11: WooCommerce setup
print_status "WooCommerce + Stripe setup:"
echo "5. Install required plugins:"
echo "   - WooCommerce"
echo "   - WooCommerce Stripe Gateway"
echo "   - Elementor (for page building)"
echo ""
echo "6. Configure Stripe:"
echo "   - Get API keys from https://dashboard.stripe.com"
echo "   - Go to Empire TCG > Settings in WordPress admin"
echo "   - Add your Stripe keys"
echo ""

# Step 12: SSL Setup
print_status "SSL Certificate setup:"
if [ "$1" != "--skip-ssl" ]; then
    echo "7. Generate SSL certificates:"
    echo "   - Update DNS: empirecardgame.com -> your server IP"
    echo "   - Run: docker-compose -f docker-compose-cms.yml run --rm certbot"
    echo "   - Update nginx config to use HTTPS"
else
    print_warning "Skipping SSL setup (--skip-ssl flag provided)"
fi

# Step 13: Content setup guide
print_status "Content Management Guide:"
echo ""
echo "?? For Non-Technical Users:"
echo "1. WordPress Admin: /wp-admin"
echo "2. Create pages:"
echo "   - Home (landing page)"
echo "   - Shop (WooCommerce store)"
echo "   - Rules (game rules with [empire_game] shortcode)"
echo "   - About (about the game)"
echo "   - Contact"
echo ""
echo "3. Add products in WooCommerce:"
echo "   - Booster packs"
echo "   - Single cards"
echo "   - Starter decks"
echo ""
echo "4. Embed the game anywhere with: [empire_game]"
echo "5. Show products with: [empire_products category='booster-packs']"
echo "6. Display leaderboard with: [empire_leaderboard]"

# Step 14: Architecture summary
print_status "Platform Architecture:"
echo ""
echo "?? Your Complete Platform:"
echo "??? empirecardgame.com/ (WordPress CMS & Store)"
echo "??? empirecardgame.com/shop/ (WooCommerce + Stripe)"
echo "??? empirecardgame.com/play/ (Blazor Game)"
echo "??? empirecardgame.com/game-api/ (Game API)"
echo "??? empirecardgame.com/wp-admin/ (Content Management)"
echo ""

# Step 15: Next steps
print_success "Deployment complete!"
echo ""
print_status "Next Steps:"
echo "1. Complete WordPress setup at your domain"
echo "2. Install and configure WooCommerce + Stripe"
echo "3. Activate Empire TCG theme and plugin"
echo "4. Create your content pages"
echo "5. Add products to your store"
echo "6. Test the game integration"
echo ""

print_status "Useful Commands:"
echo "- View logs: docker-compose -f docker-compose-cms.yml logs -f"
echo "- Restart: docker-compose -f docker-compose-cms.yml restart"
echo "- Stop: docker-compose -f docker-compose-cms.yml down"
echo "- WordPress CLI: docker-compose -f docker-compose-cms.yml exec wordpress wp"
echo ""

print_warning "Remember to:"
echo "- Set up regular backups"
echo "- Configure SSL certificates"
echo "- Set up monitoring"
echo "- Test payment processing thoroughly"

# Final status check
echo ""
print_status "Container Status:"
docker-compose -f docker-compose-cms.yml ps

echo ""
print_success "?? Empire TCG Complete Platform is ready!"
echo "Your non-technical team can now manage everything through WordPress!"