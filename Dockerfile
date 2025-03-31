# Use the SDK image for building the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the entire source code to the container
COPY . .

# Separate restore step for clarity
RUN dotnet restore Empire.Server/Empire.Server.csproj

# Publish the app to the /app/publish directory
RUN dotnet publish Empire.Server/Empire.Server.csproj -c Release -o /app/publish

# Use the ASP.NET runtime image for the final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy the published files from the build stage
COPY --from=build /app/publish .

# Expose port 80 for the backend
ENV ASPNETCORE_URLS=http://0.0.0.0:80

# Start the application when the container starts
ENTRYPOINT ["dotnet", "Empire.Server.dll"]

