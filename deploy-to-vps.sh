#!/bin/bash

# Empire TCG VPS Deployment Script
# Usage: ./deploy-to-vps.sh

VPS_HOST="138.68.188.47"
VPS_USER="root"
SSH_KEY="~/.ssh/EmpireGame"

echo "🚀 Deploying Empire TCG to VPS..."

# Create appsettings.json on VPS
echo "📝 Creating appsettings.json on VPS..."
ssh -i $SSH_KEY $VPS_USER@$VPS_HOST << 'EOF'
cat > /app/Empire.Server/appsettings.json << 'SETTINGS'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "EmpireGame"
  },
  "DeckDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "EmpireDecks"
  },
  "CardDB": {
    "ConnectionString": "mongodb+srv://admin:test123@cluster0.j0hbc7q.mongodb.net/Empire-Deckbuilder?retryWrites=true&w=majority&appName=Cluster0",
    "DatabaseName": "Empire-Deckbuilder"
  }
}
SETTINGS
EOF

# Deploy the application
echo "🔄 Deploying application..."
rsync -avz -e "ssh -i $SSH_KEY" --exclude='appsettings*.json' --exclude='bin/' --exclude='obj/' ./ $VPS_USER@$VPS_HOST:/app/

# Restart the application
echo "🔄 Restarting application..."
ssh -i $SSH_KEY $VPS_USER@$VPS_HOST << 'EOF'
cd /app
docker-compose down
docker-compose up -d --build
EOF

echo "✅ Deployment complete!"
