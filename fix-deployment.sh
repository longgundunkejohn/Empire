#!/bin/bash

echo "🔧 Fixing deployment issues..."

# Step 1: Show current git status
echo "📊 Current git status:"
git status

# Step 2: Handle the .dockerignore conflict aggressively
echo "📝 Resolving .dockerignore conflict..."
if [ -f .dockerignore ]; then
    echo "   - Backing up existing .dockerignore"
    mv .dockerignore .dockerignore.backup.$(date +%s)
fi

# Step 3: Reset any local changes that might conflict
echo "🔄 Resetting any conflicting local changes..."
git reset --hard HEAD
git clean -fd

# Step 4: Pull the latest changes
echo "📥 Pulling latest changes from repository..."
git pull origin main

# Step 5: Verify critical fixes are present
echo "🔍 Verifying fixes are applied..."
if grep -q "Action," Empire.Shared/Models/Enums/GamePhase.cs; then
    echo "   ✅ GamePhase.Action fix is present"
else
    echo "   ❌ GamePhase.Action fix is missing!"
fi

if ! grep -q "CsvHelper.*CsvHelper" Empire.Server/Empire.Server.csproj; then
    echo "   ✅ Duplicate CsvHelper reference is fixed"
else
    echo "   ❌ Duplicate CsvHelper reference still exists!"
fi

# Step 4: Clean up any existing build artifacts that might be taking space
echo "🧹 Cleaning up build artifacts..."
docker system prune -f
docker builder prune -f

# Step 5: Remove any large image directories that might be causing issues
echo "🗑️  Removing large image directories..."
rm -rf blazor-dist/wwwroot/images/Cards/ 2>/dev/null || true
rm -rf Empire.Client/wwwroot/images/Cards/ 2>/dev/null || true
find . -name "*.jpg" -size +1M -delete 2>/dev/null || true
find . -name "*.png" -size +1M -delete 2>/dev/null || true

# Step 6: Check available disk space
echo "💾 Checking disk space..."
df -h

echo "✅ Deployment fix complete!"
echo ""
echo "Next steps:"
echo "1. Run your Docker build command"
echo "2. The build should now succeed without storage issues"
echo "3. Card images should be served from external storage or CDN"
