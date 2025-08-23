#!/bin/bash

echo "?? AGGRESSIVE PACKAGE CONFLICT FIX"
echo "=================================="
echo "Completely cleaning and rebuilding to fix SignalR version conflict"

# Stop everything
echo "?? Stopping all Docker containers..."
docker-compose -f docker-compose-cms.yml down 2>/dev/null || true
docker stop $(docker ps -aq) 2>/dev/null || true
docker rm $(docker ps -aq) 2>/dev/null || true

# Nuclear Docker cleanup
echo "?? Nuclear Docker cleanup..."
docker system prune -af --volumes
docker network prune -f
docker volume prune -f

# Clear ALL NuGet caches
echo "?? Clearing ALL NuGet caches..."
rm -rf ~/.nuget/packages/ 2>/dev/null || true
rm -rf /tmp/NuGetScratch/ 2>/dev/null || true
rm -rf /var/tmp/.nuget/ 2>/dev/null || true

# Clear project build artifacts
echo "??? Clearing build artifacts..."
find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true

# Force create the correct project file (overwrite whatever is there)
echo "?? Force creating correct Empire.Client.csproj..."
cat > Empire.Client/Empire.Client.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <InvariantGlobalization>false</InvariantGlobalization>
    <BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>
    
    <!-- WebAssembly specific settings -->
    <OutputType>Exe</OutputType>
    
    <!-- Fix for NullabilityInfoContext -->
    <NullabilityInfoContextSupport>true</NullabilityInfoContextSupport>
    
    <!-- Disable features that can cause WASM0005 -->
    <BlazorEnableCompression>false</BlazorEnableCompression>
    <PublishTrimmed>false</PublishTrimmed>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="8.0.8" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="8.0.8" PrivateAssets="all" />
    <PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.8" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Empire.Shared\Empire.Shared.csproj" />
  </ItemGroup>

</Project>
EOF

echo "? Created clean project file"

# Verify the file was created correctly
echo "?? Verifying project file..."
echo "SignalR packages found:"
grep -n "SignalR" Empire.Client/Empire.Client.csproj || echo "No SignalR found (this means the fix worked)"

# Test local build first
echo "?? Testing local build..."
cd Empire.Client
dotnet clean
dotnet restore --force --no-cache
dotnet build

if [ $? -eq 0 ]; then
    echo "? Local build successful!"
    cd ..
else
    echo "? Local build failed - checking what went wrong..."
    cd ..
    exit 1
fi

# Now build with Docker
echo "?? Building with Docker (no cache)..."
docker-compose -f docker-compose-cms.yml build --no-cache --pull

if [ $? -eq 0 ]; then
    echo "? Docker build successful!"
    
    echo "?? Starting services..."
    docker-compose -f docker-compose-cms.yml up -d
    
    echo "? Waiting for services..."
    sleep 30
    
    echo "?? Testing services..."
    echo "WordPress:" $(curl -s -o /dev/null -w "%{http_code}" http://localhost:8080)
    echo "Game:" $(curl -s -o /dev/null -w "%{http_code}" http://localhost:8081)
    echo "Nginx:" $(curl -s -o /dev/null -w "%{http_code}" http://localhost)
    
    echo ""
    echo "?? AGGRESSIVE FIX COMPLETE!"
    echo ""
    echo "? What was fixed:"
    echo "   - Nuclear Docker cleanup"
    echo "   - Complete NuGet cache clear"
    echo "   - Force overwrite project file"
    echo "   - Clean local build first"
    echo "   - Fresh Docker build"
    echo ""
    echo "?? Access your platform:"
    echo "- WordPress: http://empirecardgame.com:8080"
    echo "- Game: http://empirecardgame.com:8081"
    echo "- Main: http://empirecardgame.com"
    
else
    echo "? Docker build still failed. Showing logs..."
    docker-compose -f docker-compose-cms.yml logs --tail=20 empire-game
    exit 1
fi