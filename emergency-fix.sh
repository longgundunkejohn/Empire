#!/bin/bash

echo "🚨 Emergency deployment fix - applying critical patches manually..."

# Step 1: Fix GamePhase enum to include Action
echo "🔧 Fixing GamePhase enum..."
cat > Empire.Shared/Models/Enums/GamePhase.cs << 'EOF'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Empire.Shared.Models
{
    public enum GamePhase
    {
        Strategy,
        Action,
        Battle,
        Resolution,
        Replenishment,
    }
}
EOF

echo "   ✅ GamePhase.Action added"

# Step 2: Fix duplicate CsvHelper reference
echo "🔧 Fixing duplicate CsvHelper reference..."
sed -i '/<PackageReference Include="CsvHelper" Version="33.0.1" \/>/,+2d' Empire.Server/Empire.Server.csproj

echo "   ✅ Duplicate CsvHelper reference removed"

# Step 3: Create optimized .dockerignore
echo "🔧 Creating optimized .dockerignore..."
cat > .dockerignore << 'EOF'
# Build outputs
**/bin/
**/obj/
**/out/
**/publish/

# IDE files
.vs/
.vscode/
*.user
*.suo
*.cache
*.tmp

# OS files
.DS_Store
Thumbs.db

# Git
.git/
.gitignore

# Node modules (if any)
node_modules/

# Large image assets that can cause storage issues
**/wwwroot/images/Cards/
**/wwwroot/images/cards/
**/Images/
**/images/
blazor-dist/
**/blazor-dist/
**/*.jpg
**/*.jpeg
**/*.png
**/*.gif
**/*.bmp
**/*.tiff
**/*.webp
# Exclude specific problematic paths mentioned in error
/src/blazor-dist/wwwroot/images/Cards/

# Logs
*.log

# Temporary files
*.tmp
*.temp

# Certificate files (should be mounted at runtime)
certbot/

# Documentation
*.md
README*

# Test files
**/*Test*/
**/*Tests*/
EOF

echo "   ✅ Optimized .dockerignore created"

# Step 4: Create optimized Dockerfile
echo "🔧 Creating optimized Dockerfile..."
cat > Dockerfile << 'EOF'
# ----------- Build the application -----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first for better layer caching
COPY Empire.Shared/Empire.Shared.csproj Empire.Shared/
COPY Empire.Client/Empire.Client.csproj Empire.Client/
COPY Empire.Server/Empire.Server.csproj Empire.Server/

# Restore dependencies
RUN dotnet restore Empire.Server/Empire.Server.csproj

# Copy only essential source files (no images)
COPY Empire.Shared/ Empire.Shared/
COPY Empire.Client/ Empire.Client/
COPY Empire.Server/ Empire.Server/
COPY Empire.sln .
COPY nginx/ nginx/

# Remove any image directories that might have been copied
RUN rm -rf Empire.Client/wwwroot/images/Cards/ || true
RUN rm -rf Empire.Client/wwwroot/images/cards/ || true
RUN rm -rf blazor-dist/ || true

# Publish server, which also triggers client publish via post-build hook
RUN dotnet publish Empire.Server/Empire.Server.csproj -c Release -o /app/publish --no-restore

# ----------- Runtime image -----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "Empire.Server.dll"]
EOF

echo "   ✅ Optimized Dockerfile created"

# Step 5: Clean up problematic files
echo "🧹 Cleaning up problematic files..."
rm -rf blazor-dist/wwwroot/images/Cards/ 2>/dev/null || true
rm -rf Empire.Client/wwwroot/images/Cards/ 2>/dev/null || true
find . -name "*.jpg" -size +1M -delete 2>/dev/null || true
find . -name "*.png" -size +1M -delete 2>/dev/null || true

# Step 6: Clean Docker
echo "🧹 Cleaning Docker cache..."
docker system prune -f
docker builder prune -f

# Step 7: Test the fixes
echo "🔍 Testing fixes..."
echo "Checking GamePhase enum:"
grep -n "Action," Empire.Shared/Models/Enums/GamePhase.cs || echo "❌ GamePhase.Action not found"

echo "Checking for duplicate CsvHelper:"
grep -c "CsvHelper" Empire.Server/Empire.Server.csproj

echo "💾 Current disk space:"
df -h

echo ""
echo "🚨 Emergency fixes applied!"
echo "Now run: docker-compose up --build -d"
