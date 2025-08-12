# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Install wasm-tools workload for Blazor WebAssembly
RUN dotnet workload install wasm-tools

# Copy project files for dependency restoration
COPY Empire.Shared/Empire.Shared.csproj Empire.Shared/
COPY Empire.Client/Empire.Client.csproj Empire.Client/
COPY Empire.Server/Empire.Server.csproj Empire.Server/
COPY Empire.sln .

# Restore dependencies
RUN dotnet restore Empire.Server/Empire.Server.csproj

# Copy all source code
COPY Empire.Shared/ Empire.Shared/
COPY Empire.Client/ Empire.Client/
COPY Empire.Server/ Empire.Server/

# Build and publish the Blazor Client (WebAssembly)
RUN dotnet publish Empire.Client/Empire.Client.csproj -c Release -o /app/client --no-restore

# Build and publish the Server
RUN dotnet publish Empire.Server/Empire.Server.csproj -c Release -o /app/server --no-restore

# Copy Blazor client files to server's wwwroot
RUN mkdir -p /app/server/wwwroot
RUN cp -r /app/client/* /app/server/wwwroot/

# Use the official .NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy the published application
COPY --from=build /app/server .

# Create a non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Expose port 8080 (non-privileged port)
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the application
ENTRYPOINT ["dotnet", "Empire.Server.dll"]
