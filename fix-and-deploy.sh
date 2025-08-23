#!/bin/bash

echo "?? EMPIRE TCG - AUTOMATED DEPLOYMENT FIX"
echo "========================================"
echo "Fixing all merge conflicts, duplicates, and deploying..."
echo ""

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

print_status() { echo -e "${BLUE}[INFO]${NC} $1"; }
print_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
print_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
print_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Step 1: Fix the Dockerfile merge conflict
print_status "Creating clean Dockerfile..."
cat > Dockerfile << 'EOF'
# Use the official .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Install Python and WebAssembly workload
RUN apt-get update && apt-get install -y python3 python3-pip && \
    ln -s /usr/bin/python3 /usr/bin/python

# Install WASM workload with proper version handling
RUN dotnet workload install wasm-tools --skip-manifest-update

# Copy project files
COPY Empire.Shared/Empire.Shared.csproj Empire.Shared/
COPY Empire.Client/Empire.Client.csproj Empire.Client/
COPY Empire.Server/Empire.Server.csproj Empire.Server/
COPY Empire.sln .

# Restore dependencies
RUN dotnet restore Empire.Server/Empire.Server.csproj
RUN dotnet restore Empire.Client/Empire.Client.csproj

# Copy source code
COPY Empire.Shared/ Empire.Shared/
COPY Empire.Client/ Empire.Client/
COPY Empire.Server/ Empire.Server/

# Build and publish the Blazor Client (WebAssembly) with simplified settings
RUN dotnet publish Empire.Client/Empire.Client.csproj -c Release -o /app/client \
    --no-restore \
    -p:BlazorEnableCompression=false

# Build and publish the ASP.NET Core Server
RUN dotnet publish Empire.Server/Empire.Server.csproj -c Release -o /app/publish \
    --no-restore

# Use the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create data directory for SQLite
RUN mkdir -p /app/data

# Copy published server application
COPY --from=build /app/publish .

# Copy the published Blazor client to wwwroot
COPY --from=build /app/client ./wwwroot

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "Empire.Server.dll"]
EOF

print_success "? Created clean Dockerfile"

# Step 2: Fix the Empire.Client.csproj duplicate SignalR package
print_status "Fixing Empire.Client.csproj duplicate packages..."
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

print_success "? Fixed Empire.Client.csproj - removed duplicate SignalR package"

# Step 3: Clean up Docker
print_status "Cleaning up Docker..."
docker-compose -f docker-compose-cms.yml down 2>/dev/null || true
docker system prune -f

# Step 4: Create directories
print_status "Creating necessary directories..."
mkdir -p {wordpress,mysql-data,game-data,game-logs,certbot/{conf,www}}
mkdir -p nginx/ssl
chmod -R 755 wordpress mysql-data game-data certbot

# Step 5: Start deployment
print_status "Starting Docker build..."

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    print_error "Docker is not running. Please start Docker first."
    exit 1
fi

# Build the game container first
print_status "Building Empire game container..."
docker-compose -f docker-compose-cms.yml build --no-cache empire-game

# Check if build succeeded
if [ $? -eq 0 ]; then
    print_success "? Empire TCG game container built successfully!"
else
    print_error "? Game container build failed"
    echo ""
    echo "Showing build logs:"
    docker-compose -f docker-compose-cms.yml logs empire-game
    exit 1
fi

# Build other containers
print_status "Building remaining containers..."
docker-compose -f docker-compose-cms.yml build

# Start all services
print_status "Starting all services..."
docker-compose -f docker-compose-cms.yml up -d

# Wait for services to start
print_status "Waiting for services to start..."
sleep 30

# Check container status
print_status "Checking container status..."
docker-compose -f docker-compose-cms.yml ps

# Test connectivity
print_status "Testing connectivity..."

# Test WordPress
WP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8080 2>/dev/null || echo "000")
if [[ "$WP_STATUS" == "200" || "$WP_STATUS" == "302" ]]; then
    print_success "? WordPress accessible on port 8080"
else
    print_warning "?? WordPress status: $WP_STATUS"
fi

# Test Game
GAME_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8081 2>/dev/null || echo "000")
if [[ "$GAME_STATUS" == "200" ]]; then
    print_success "? Game accessible on port 8081"
else
    print_warning "?? Game status: $GAME_STATUS"
fi

# Test Nginx
NGINX_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost 2>/dev/null || echo "000")
if [[ "$NGINX_STATUS" == "200" || "$NGINX_STATUS" == "302" ]]; then
    print_success "? Nginx proxy working on port 80"
else
    print_warning "?? Nginx status: $NGINX_STATUS"
fi

echo ""
print_success "?? DEPLOYMENT COMPLETE!"
echo ""
echo "?? ACCESS YOUR PLATFORM:"
echo "========================"
echo "WordPress: http://empirecardgame.com:8080 (or http://138.68.188.47:8080)"
echo "Game: http://empirecardgame.com:8081 (or http://138.68.188.47:8081)"  
echo "Main Site: http://empirecardgame.com (or http://138.68.188.47)"
echo ""
echo "?? NEXT STEPS:"
echo "=============="
echo "1. Complete WordPress setup"
echo "   - Go to: http://empirecardgame.com:8080"
echo "   - Database Host: mysql"
echo "   - Database: empire_wordpress"
echo "   - Username: empire_user"
echo "   - Password: empire_secure_2024"
echo ""
echo "2. Install plugins:"
echo "   - WooCommerce"
echo "   - WooCommerce Stripe Gateway"
echo "   - Elementor"
echo ""
echo "3. Configure Stripe API keys"
echo ""

# Show logs if there are issues
if [[ "$WP_STATUS" != "200" && "$WP_STATUS" != "302" ]] || [[ "$GAME_STATUS" != "200" ]]; then
    echo ""
    print_warning "?? Some services may need attention. Checking logs..."
    echo ""
    echo "WordPress logs:"
    docker-compose -f docker-compose-cms.yml logs --tail=10 wordpress
    echo ""
    echo "Game logs:"  
    docker-compose -f docker-compose-cms.yml logs --tail=10 empire-game
fi

print_status "?? Deployment completed successfully!"
echo ""
echo "?? Management commands:"
echo "- View logs: docker-compose -f docker-compose-cms.yml logs -f"
echo "- Restart: docker-compose -f docker-compose-cms.yml restart"
echo "- Stop: docker-compose -f docker-compose-cms.yml down"