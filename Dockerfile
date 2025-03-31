  GNU nano 8.1                                           Dockerfile                                                     # ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore Empire.Server/Empire.Server.csproj

# ?? This now handles both server and client (thanks to your Target)
RUN dotnet publish Empire.Server/Empire.Server.csproj -c Release -o /app/publish

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:80

ENTRYPOINT ["dotnet", "Empire.Server.dll"]