# Empire TCG Deployment Fix Plan

## 🚨 CRITICAL ISSUE IDENTIFIED

**Root Cause**: The Blazor WebAssembly client files (including `icudt.dat`) are not being properly built and included in the Docker container.

**Problem**: The current Dockerfile publishes the server project, which has a post-build target to publish the client, but this happens in the wrong order and the client files aren't copied to the final container.

## 🔧 IMMEDIATE FIXES REQUIRED

### 1. Fix Dockerfile Build Process

**Current Issue**: The Dockerfile only publishes the server, expecting the post-build target to handle the client, but the client files don't make it to the final container.

**Solution**: Explicitly build and publish both client and server in the Dockerfile.

### 2. Fix nginx Configuration

**Current Issue**: nginx doesn't have proper MIME types for Blazor WebAssembly files (.dat, .wasm, .dll, etc.)

**Solution**: Add proper MIME type handling and caching headers for Blazor files.

### 3. Fix Build Target Order

**Current Issue**: The post-build target in Empire.Server.csproj runs after publish, but the files aren't included in the container.

**Solution**: Restructure the build process to ensure client files are available during the Docker build.

## 📋 STEP-BY-STEP FIX PLAN

### Phase 1: Fix Dockerfile (CRITICAL - 30 minutes)

1. **Update Dockerfile to properly build Blazor client**
2. **Ensure all framework files are included**
3. **Add proper file permissions**

### Phase 2: Fix nginx Configuration (CRITICAL - 15 minutes)

1. **Add MIME types for Blazor files**
2. **Add proper caching headers**
3. **Add compression for .wasm files**

### Phase 3: Update Build Process (IMPORTANT - 15 minutes)

1. **Fix the post-build target timing**
2. **Ensure proper file copying**

### Phase 4: Update Deployment Script (IMPORTANT - 15 minutes)

1. **Add build verification steps**
2. **Add health checks**
3. **Add rollback capability**

## 🛠️ TECHNICAL IMPLEMENTATION

### New Dockerfile Structure
```dockerfile
# Build both client and server properly
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy and restore all projects
COPY Empire.Shared/Empire.Shared.csproj Empire.Shared/
COPY Empire.Client/Empire.Client.csproj Empire.Client/
COPY Empire.Server/Empire.Server.csproj Empire.Server/
RUN dotnet restore Empire.Server/Empire.Server.csproj

# Copy source code
COPY Empire.Shared/ Empire.Shared/
COPY Empire.Client/ Empire.Client/
COPY Empire.Server/ Empire.Server/
COPY Empire.sln .

# Build client first (critical fix)
RUN dotnet publish Empire.Client/Empire.Client.csproj -c Release -o /app/client-publish

# Build server and copy client files
RUN dotnet publish Empire.Server/Empire.Server.csproj -c Release -o /app/server-publish
RUN cp -r /app/client-publish/* /app/server-publish/wwwroot/

# Runtime container with all files
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/server-publish .
```

### nginx Configuration Updates
```nginx
# Add to server block
location ~* \.(wasm|dat|dll|pdb|blat)$ {
    add_header Cache-Control "public, max-age=31536000, immutable";
    add_header Access-Control-Allow-Origin "*";
    gzip_static on;
    expires 1y;
}

# Add MIME types
location ~* \.dat$ {
    add_header Content-Type "application/octet-stream";
}

location ~* \.wasm$ {
    add_header Content-Type "application/wasm";
}
```

## ⚡ EMERGENCY DEPLOYMENT SCRIPT

### Quick Fix Commands
```bash
# 1. Stop current deployment
docker-compose down

# 2. Clean build cache
docker system prune -f
docker builder prune -f

# 3. Rebuild with fixed Dockerfile
docker-compose build --no-cache

# 4. Deploy with health checks
docker-compose up -d

# 5. Verify deployment
curl -I https://empirecardgame.com/_framework/icudt.dat
```

## 🎯 SUCCESS CRITERIA

### Deployment Fixed When:
1. ✅ `https://empirecardgame.com/_framework/icudt.dat` returns 200 OK
2. ✅ Main page loads without "Failed to start platform" error
3. ✅ Blazor WebAssembly initializes successfully
4. ✅ Login/register pages are accessible
5. ✅ Deck builder loads properly

## 📊 ESTIMATED TIMELINE

- **Phase 1 (Dockerfile Fix)**: 30 minutes
- **Phase 2 (nginx Fix)**: 15 minutes  
- **Phase 3 (Build Process)**: 15 minutes
- **Phase 4 (Deployment Script)**: 15 minutes
- **Testing & Verification**: 15 minutes

**Total Time**: ~90 minutes to fully working deployment

## 🚀 POST-FIX ACTIONS

### Once Deployment is Fixed:
1. **Verify all pages load**: Test login, deck builder, lobby
2. **Connect manual game**: Link lobby to the manual play system
3. **Add health monitoring**: Set up deployment health checks
4. **Document process**: Update deployment documentation

## 💡 PREVENTION MEASURES

### To Prevent Future Issues:
1. **Add deployment health checks** to verify Blazor files
2. **Add automated testing** of the deployment process
3. **Create staging environment** for testing deployments
4. **Add monitoring** for critical file availability

## 🎯 IMMEDIATE NEXT STEPS

1. **Fix Dockerfile** (highest priority)
2. **Update nginx config** 
3. **Test deployment locally** with Docker
4. **Deploy to production**
5. **Verify all functionality**

**Bottom Line**: This is a classic Blazor WebAssembly deployment issue. The fix is well-understood and should take about 90 minutes to implement and verify.

Once fixed, the application will be fully functional and ready for users.
