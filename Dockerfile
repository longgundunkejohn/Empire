FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

# ✅ Separate restore step for clarity
RUN dotnet restore Empire.Server/Empire.Server.csproj

# ✅ Then publish
RUN dotnet publish Empire.Server/Empire.Server.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:80

ENTRYPOINT ["dotnet", "Empire.Server.dll"]

