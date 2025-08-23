#!/bin/bash

echo "?? AUTOMATED MERGE CONFLICT RESOLVER"
echo "==================================="
echo "Fixing Dockerfile merge conflict automatically..."

# Check if we're in a merge conflict state
if ! git status | grep -q "both modified"; then
    echo "? No merge conflicts detected. You're good to go!"
    exit 0
fi

echo "?? Resolving Dockerfile merge conflict..."

# Create the corrected Dockerfile (keeping WebAssembly fixes)
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

echo "? Created fixed Dockerfile with WebAssembly build fixes"

# Add all files and commit the merge
echo "?? Adding all files and committing merge..."
git add .
git commit -m "Merge: Resolve Dockerfile conflict - keep WebAssembly build fixes + add card images"

# Push changes
echo "?? Pushing changes to remote..."
git push origin main

echo ""
echo "?? MERGE CONFLICT RESOLVED SUCCESSFULLY!"
echo ""
echo "? What was fixed:"
echo "   - Kept WebAssembly build fixes in Dockerfile"
echo "   - Added all card images and new files"
echo "   - Committed and pushed changes"
echo ""
echo "?? Ready to deploy! Run:"
echo "   ./deploy-fixed.sh"