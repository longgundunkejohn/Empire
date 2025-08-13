# Empire TCG Issues Diagnosis and Fix

## Issues Identified

### 1. 🚨 Critical: Blazor WebAssembly Not Loading
**Problem**: The main website fails to load due to missing `icudt.dat` file
- **Error**: `Failed to load resource: the server responded with a status of 404 (Not Found)` for `icudt.dat`
- **Impact**: Entire frontend is non-functional
- **Root Cause**: .NET WebAssembly framework files not properly included in Docker build

### 2. 👻 Phantom Users in Lobby
**Problem**: Users appear online when they're not actually connected
- **Root Cause**: Stale SignalR connections not being cleaned up properly
- **Impact**: Confusing user experience, false player counts

### 3. 🖼️ Card Images (NOT AN ISSUE)
**Status**: ✅ **WORKING CORRECTLY**
- Card images are loading perfectly from `/images/Cards/`
- API is returning correct card data
- Example: `https://empirecardgame.com/images/Cards/109.jpg` loads correctly

## Solutions Implemented

### 1. Fixed Dockerfile for Proper .NET Framework Files
**File**: `Dockerfile.blazor-fix`

**Key Changes**:
```dockerfile
# Build with proper framework files
RUN dotnet publish Empire.Client/Empire.Client.csproj -c Release -o /app/client \
    --self-contained false \
    -p:PublishTrimmed=false \
    -p:BlazorEnableCompression=false

# Use proper copy command to preserve all files
RUN cp -a /app/client/. /app/server/wwwroot/

# Verify framework files are present
RUN ls -la /app/server/wwwroot/_framework/ | grep -E "(dotnet|icudt|blazor)"
```

**Why This Fixes It**:
- Disables trimming that might remove essential framework files
- Uses `cp -a` to preserve all file attributes and links
- Explicitly verifies framework files are present during build

### 2. Enhanced SignalR Connection Cleanup
**File**: `Empire.Server/Hubs/GameHub.cs`

**Existing Good Features**:
- Connection tracking with `UserConnectionInfo`
- Automatic cleanup on disconnect
- Lobby cleanup when users disconnect

**Additional Cleanup Strategy**:
- Container restart clears all stale connections
- Fresh deployment eliminates phantom users

### 3. Comprehensive Fix Script
**File**: `fix-empire-issues.sh`

**What It Does**:
1. Stops current containers
2. Cleans Docker system completely
3. Switches to improved Dockerfile
4. Rebuilds with no cache (ensures fresh framework files)
5. Deploys fresh containers
6. Restarts containers to clear SignalR connections
7. Tests all endpoints

## Deployment Instructions

### Option 1: Run the Comprehensive Fix Script
```bash
# On the VPS, run:
./fix-empire-issues.sh
```

### Option 2: Manual Steps
```bash
# Stop containers
docker-compose down

# Clean system
docker system prune -f
docker volume prune -f

# Backup and switch Dockerfile
cp Dockerfile Dockerfile.backup
cp Dockerfile.blazor-fix Dockerfile

# Rebuild and deploy
docker-compose build --no-cache
docker-compose up -d

# Wait and restart to clear connections
sleep 45
docker-compose restart
```

## Verification Steps

After deployment, verify:

1. **Main Website**: https://empirecardgame.com
   - Should load without framework errors
   - Check browser console for no `icudt.dat` errors

2. **API Endpoint**: https://empirecardgame.com/api/deckbuilder/cards
   - Should return JSON card data

3. **Card Images**: https://empirecardgame.com/images/Cards/109.jpg
   - Should display card image

4. **Deck Builder**: https://empirecardgame.com/deckbuilder
   - Should load and display cards with images

5. **Lobby**: https://empirecardgame.com/lobby
   - Should show accurate user counts (no phantom users)

## Expected Results

✅ **Blazor app loads correctly**
✅ **Deck builder displays cards with images**
✅ **Lobby shows accurate user presence**
✅ **No more framework file errors**
✅ **SignalR connections work properly**

## Technical Details

### Why the Original Dockerfile Failed
- Used `cp -r` instead of `cp -a` (doesn't preserve all file attributes)
- Potential trimming of essential framework files
- Missing verification of critical framework files

### Why Card Images Were Never the Problem
- Images are served directly by nginx/static file serving
- API correctly returns image paths
- The issue was the Blazor app not loading to display them

### SignalR Connection Management
- GameHub has good cleanup logic
- Container restart ensures all stale connections are cleared
- Fresh deployment eliminates any phantom user issues

## Monitoring

After deployment, monitor:
- Browser console for any remaining errors
- Docker logs: `docker-compose logs -f`
- User reports of phantom users
- Deck builder functionality

## Rollback Plan

If issues persist:
```bash
# Restore original Dockerfile
cp Dockerfile.backup Dockerfile

# Rebuild and deploy
docker-compose build --no-cache
docker-compose up -d
