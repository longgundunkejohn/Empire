#!/bin/bash

echo "?? EMPIRE TCG WORKSPACE CLEANUP"
echo "==============================="
echo "Organizing files for Phase 1 CMS development"
echo ""

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

print_status() { echo -e "${BLUE}[CLEANUP]${NC} $1"; }
print_success() { echo -e "${GREEN}[ORGANIZED]${NC} $1"; }
print_warning() { echo -e "${YELLOW}[MOVED]${NC} $1"; }

# Create organized directory structure
print_status "Creating organized directory structure..."
mkdir -p docs
mkdir -p scripts/deployment
mkdir -p scripts/cms
mkdir -p archive

# Move deployment scripts to scripts directory
print_status "Organizing deployment scripts..."
if [ -f "deploy-complete-cms.sh" ]; then
    mv deploy-complete-cms.sh scripts/deployment/
    print_warning "deploy-complete-cms.sh ? scripts/deployment/"
fi

if [ -f "setup-wordpress-cms.sh" ]; then
    mv setup-wordpress-cms.sh scripts/cms/
    print_warning "setup-wordpress-cms.sh ? scripts/cms/"
fi

if [ -f "setup-complete-platform.sh" ]; then
    mv setup-complete-platform.sh scripts/cms/
    print_warning "setup-complete-platform.sh ? scripts/cms/"
fi

# Move any additional CMS-related scripts
for script in configure-stripe-fast.sh deploy-wordpress-fast.sh auto-setup-wordpress.sh deploy-empire-complete.sh; do
    if [ -f "$script" ]; then
        mv "$script" scripts/cms/
        print_warning "$script ? scripts/cms/"
    fi
done

# Move documentation to docs directory
print_status "Organizing documentation..."
if [ -f "EMPIRE_CONQUEST_ROADMAP_TO_PERFECTION.md" ]; then
    mv EMPIRE_CONQUEST_ROADMAP_TO_PERFECTION.md archive/
    print_warning "EMPIRE_CONQUEST_ROADMAP_TO_PERFECTION.md ? archive/ (superseded by roadmap.md)"
fi

if [ -f "README-DEPLOYMENT.md" ]; then
    mv README-DEPLOYMENT.md docs/deployment.md
    print_warning "README-DEPLOYMENT.md ? docs/deployment.md"
fi

if [ -f "SSL_DEPLOYMENT_GUIDE.md" ]; then
    mv SSL_DEPLOYMENT_GUIDE.md docs/ssl-guide.md
    print_warning "SSL_DEPLOYMENT_GUIDE.md ? docs/ssl-guide.md"
fi

# Archive old/duplicate files
print_status "Archiving superseded files..."
for file in fix-*.sh renew-*.sh empire-*.sh; do
    if [ -f "$file" ]; then
        mv "$file" archive/
        print_warning "$file ? archive/ (superseded)"
    fi
done

# Make all scripts executable
print_status "Setting script permissions..."
chmod +x scripts/deployment/*.sh 2>/dev/null
chmod +x scripts/cms/*.sh 2>/dev/null
print_success "All scripts are now executable"

# Create quick access symlinks for primary scripts
print_status "Creating quick access links..."
if [ -f "scripts/deployment/deploy-complete-cms.sh" ]; then
    ln -sf scripts/deployment/deploy-complete-cms.sh deploy-cms.sh
    print_success "deploy-cms.sh ? Quick link to main deployment script"
fi

if [ -f "scripts/cms/configure-stripe-fast.sh" ]; then
    ln -sf scripts/cms/configure-stripe-fast.sh configure-stripe.sh
    print_success "configure-stripe.sh ? Quick link to Stripe setup"
fi

# Update the primary roadmap with current status
print_status "Updating roadmap with current status..."
cat >> roadmap.md << 'EOF'

---

## ?? **WORKSPACE ORGANIZATION COMPLETE**

### **?? New Folder Structure:**
```
Empire TCG/
??? docs/                          # All documentation
?   ??? roadmap.md                 # Master roadmap (this file)
?   ??? deployment.md              # Deployment guides
?   ??? ssl-guide.md              # SSL setup instructions
??? scripts/
?   ??? deployment/                # Infrastructure deployment
?   ??? cms/                       # WordPress & CMS scripts
??? archive/                       # Superseded files
??? wordpress/                     # WordPress CMS files
??? Empire.Client/                 # Blazor WebAssembly frontend
??? Empire.Server/                 # ASP.NET Core backend
??? Empire.Shared/                 # Shared models and logic
??? deploy-cms.sh                  # Quick link to main deployment
??? configure-stripe.sh            # Quick link to Stripe setup
```

### **?? Ready for Phase 1: CMS Integration**
Workspace is now organized and ready for the 14-15 hour CMS development sprint!

**Next command to run:**
```bash
./deploy-cms.sh
```

EOF

print_success "Roadmap updated with organization status"

# Create a quick reference card
cat > QUICK_START.md << 'EOF'
# ?? EMPIRE TCG - QUICK START

## **For CMS Development (Phase 1):**
```bash
# Deploy WordPress + Stripe + Game integration
./deploy-cms.sh

# Configure Stripe payments
./configure-stripe.sh
```

## **Key Files:**
- `roadmap.md` - Master development roadmap
- `docs/` - All documentation
- `scripts/` - All deployment and setup scripts
- `wordpress/` - CMS theme and plugins

## **WordPress Access:**
- Admin: http://empirecardgame.com/wp-admin
- Main Site: http://empirecardgame.com
- Game: http://empirecardgame.com/play

## **Current Priority:**
**Phase 1: CMS Integration (14-15 hours)**
Focus on WordPress + WooCommerce + Stripe + Game embedding
EOF

print_success "Created QUICK_START.md for easy reference"

echo ""
print_success "?? WORKSPACE CLEANUP COMPLETE!"
echo ""
echo "?? Summary:"
echo "? Organized all deployment scripts"
echo "? Moved documentation to docs/"
echo "? Archived superseded files"
echo "? Created quick access links"
echo "? Updated roadmap with current status"
echo ""
echo "?? Ready to begin Phase 1: CMS Integration!"
echo "Run: ./deploy-cms.sh to start"