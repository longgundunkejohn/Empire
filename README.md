# Empire TCG - Trading Card Game

A modern web-based implementation of the Empire Trading Card Game built with .NET 8, Blazor WebAssembly, and SignalR.

## 🎮 Game Overview

Empire is a 2-player strategic card game where players fight for control of three territories, managing both Army and Civic decks to reduce their opponent's Morale to 0.

### Key Game Mechanics
- **Dual Deck System**: Army cards (30) and Civic cards (15)
- **Initiative-Based Turns**: Dynamic turn order based on actions
- **Territory Control**: Fight for control of three strategic territories
- **Tier System**: Unlock more powerful cards by settling territories
- **Multiple Card Types**: Units, Tactics, Battle Tactics, Chronicles, Villagers, and Settlements

## 🏗️ Architecture

### Frontend (Empire.Client)
- **Blazor WebAssembly** for rich client-side interactions
- **SignalR Client** for real-time multiplayer communication
- **Bootstrap** for responsive UI design
- **Component-based architecture** for reusable game elements

### Backend (Empire.Server)
- **ASP.NET Core 8** web API
- **SignalR Hubs** for real-time game state synchronization
- **Entity Framework Core** for data persistence
- **JWT Authentication** for secure user sessions

### Shared (Empire.Shared)
- **Common models** and DTOs
- **Game logic** and validation
- **Serialization** optimizations

## 🚀 Quick Start

### Prerequisites
- Docker and Docker Compose
- .NET 8 SDK (for development)

### Deployment

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd empire-tcg
   ```

2. **Run the deployment script**
   ```bash
   chmod +x deploy.sh
   ./deploy.sh
   ```

3. **Access the application**
   - Local: http://localhost
   - Production: Configure domain in `nginx/nginx.conf` and `docker-compose.yml`

### Development

1. **Install .NET 8 SDK**
2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Run the server**
   ```bash
   cd Empire.Server
   dotnet run
   ```

4. **Run the client** (in another terminal)
   ```bash
   cd Empire.Client
   dotnet run
   ```

## 🎯 Current Features

### ✅ Implemented
- **User Authentication**: Registration, login, JWT tokens
- **Lobby System**: Create and join game lobbies
- **Deck Builder**: Build and manage custom decks
- **Card Database**: Complete card data with images
- **Game Board**: Visual representation of territories and zones
- **Real-time Communication**: SignalR for live game updates
- **Responsive Design**: Works on desktop and mobile

### 🚧 In Progress
- **Core Game Logic**: Turn management, card effects, win conditions
- **Game State Management**: Complete rule enforcement
- **Card Interactions**: Abilities, triggers, and effects
- **Tournament System**: Organized competitive play

### 📋 Planned
- **AI Opponents**: Single-player practice mode
- **Spectator Mode**: Watch ongoing games
- **Replay System**: Review past games
- **Advanced Deck Analytics**: Deck performance metrics
- **Mobile App**: Native mobile experience

## 🗂️ Project Structure

```
Empire/
├── Empire.Client/          # Blazor WebAssembly frontend
│   ├── Components/         # Reusable UI components
│   ├── Pages/             # Application pages
│   ├── Services/          # Client-side services
│   └── wwwroot/           # Static assets
├── Empire.Server/         # ASP.NET Core backend
│   ├── Controllers/       # API endpoints
│   ├── Services/          # Business logic
│   ├── Hubs/             # SignalR hubs
│   └── Data/             # Database context
├── Empire.Shared/         # Shared models and logic
│   ├── Models/           # Data models
│   ├── DTOs/             # Data transfer objects
│   └── Enums/            # Shared enumerations
├── nginx/                # Reverse proxy configuration
├── Dockerfile            # Container build instructions
├── docker-compose.yml    # Multi-container orchestration
└── deploy.sh            # Deployment script
```

## 🔧 Configuration

### Environment Variables
- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `ASPNETCORE_URLS`: Server binding URLs
- Database connection strings in `appsettings.json`

### SSL/HTTPS Setup
1. Update `nginx/nginx.conf` with your domain
2. Update `docker-compose.yml` with your email and domain
3. Run: `docker-compose run --rm certbot`

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🎮 Game Rules

For complete game rules, see the included Empire Rules Explainer PDF or visit the official Empire TCG website.

## 🐛 Issues & Support

- Report bugs via GitHub Issues
- Join our Discord community for support
- Check the Wiki for detailed documentation
