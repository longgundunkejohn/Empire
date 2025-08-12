# Empire TCG Deployment Script for Windows
param(
    [switch]$NoBuild,
    [switch]$Logs
)

Write-Host "🚀 Starting Empire TCG deployment..." -ForegroundColor Green

# Check if Docker is running
try {
    docker info | Out-Null
    Write-Host "✅ Docker is running" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker is not running. Please start Docker Desktop and try again." -ForegroundColor Red
    exit 1
}

# Check if docker-compose is available
try {
    docker-compose --version | Out-Null
    Write-Host "✅ Docker Compose is available" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker Compose is not available. Please install Docker Desktop." -ForegroundColor Red
    exit 1
}

# Stop existing containers
Write-Host "🛑 Stopping existing containers..." -ForegroundColor Yellow
docker-compose down --remove-orphans

if (-not $NoBuild) {
    # Remove old images to ensure fresh build
    Write-Host "🧹 Cleaning up old images..." -ForegroundColor Yellow
    docker image prune -f
    
    # Build the application
    Write-Host "🏗️ Building Empire TCG..." -ForegroundColor Yellow
    docker-compose build --no-cache
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed. Please check the output above." -ForegroundColor Red
        exit 1
    }
}

# Create SSL directory
Write-Host "📁 Creating SSL directory..." -ForegroundColor Yellow
if (-not (Test-Path "nginx/ssl")) {
    New-Item -ItemType Directory -Path "nginx/ssl" -Force | Out-Null
}

# Start the application
Write-Host "🚀 Starting Empire TCG..." -ForegroundColor Yellow
docker-compose up -d

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to start containers. Check logs with: docker-compose logs" -ForegroundColor Red
    exit 1
}

# Wait for services to be ready
Write-Host "⏳ Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Check if services are running
$runningContainers = docker-compose ps --services --filter "status=running"
if ($runningContainers) {
    Write-Host "✅ Empire TCG is now running!" -ForegroundColor Green
    Write-Host ""
    Write-Host "🌐 Access the application at:" -ForegroundColor Cyan
    Write-Host "   HTTP:  http://localhost" -ForegroundColor White
    Write-Host "   HTTPS: https://localhost (after SSL setup)" -ForegroundColor White
    Write-Host ""
    Write-Host "📋 Useful commands:" -ForegroundColor Cyan
    Write-Host "   View logs:     docker-compose logs -f" -ForegroundColor White
    Write-Host "   Stop:          docker-compose down" -ForegroundColor White
    Write-Host "   Restart:       .\deploy.ps1 -NoBuild" -ForegroundColor White
    Write-Host ""
    Write-Host "⚠️  Note: For production deployment:" -ForegroundColor Yellow
    Write-Host "   1. Update nginx/nginx.conf with your domain name" -ForegroundColor White
    Write-Host "   2. Update docker-compose.yml with your email and domain" -ForegroundColor White
    Write-Host "   3. Run: docker-compose run --rm certbot" -ForegroundColor White
    
    if ($Logs) {
        Write-Host ""
        Write-Host "📋 Showing logs (Ctrl+C to exit)..." -ForegroundColor Yellow
        docker-compose logs -f
    }
} else {
    Write-Host "❌ Failed to start services. Check logs with: docker-compose logs" -ForegroundColor Red
    docker-compose logs --tail=50
    exit 1
}
