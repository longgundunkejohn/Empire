# Empire TCG - Complete Implementation Roadmap

## Game Rules Summary

### Core Mechanics
- **Players**: 2 players fighting for control of 3 territories
- **Starting Morale**: 25 (lose when reduced to 0)
- **Decks**: Army Deck (30 cards) + Civic Deck (15 cards)
- **Opening Hand**: 4 Army + 3 Civic cards (with mulligan option)
- **Initiative System**: Players pass initiative back and forth with each action
- **Phases**: Strategy → Battle → Replenishment (repeat until game ends)

### Initiative Rules
- Each action passes initiative to opponent
- Players can pass if they don't want to act
- Both players must pass consecutively to advance to next phase
- Initiative tracker shows who can currently act

### Game Phases

#### Strategy Phase Actions
- **Deploy Army Card**: Pay costs, play from hand
- **Play Villager**: Once per round, civic card to heartland
- **Settle Territory**: Once per round, civic card to occupied territory
- **Activate Abilities**: Non-maneuver abilities
- **Commit Units**: Once per round, move units between heartland/territories

#### Battle Phase Actions
- **Deploy Army Card**: Only Battle Tactics and cards that explicitly allow it
- **Activate Abilities**: Including Maneuvers (once per round)
- **Combat**: Simultaneous in all territories, excess damage to Morale

#### Replenishment Phase
- Resolve card effects
- Unexert units and villagers
- Draw 1 Army OR 2 Civic cards
- Pass initiative tracker
- Remove damage and temporary effects

### Card Types & Mechanics

#### Army Cards
- **Units**: Attack/Defense values, can be in Heartland/Advancing/Occupying
- **Tactics**: Single-use effects, go to graveyard
- **Battle Tactics**: Can be played in Strategy OR Battle phase
- **Chronicles**: Continuous effects, escalate each round, culminate when counters ≥ cost

#### Civic Cards
- **Villagers**: Generate mana when exerted, abilities only in heartland
- **Settlements**: Abilities only when settling territories
- Can play settlements as villagers, can settle with villagers

#### Tier System
- Start at Tier I, advance one tier per settled territory (max Tier IV)
- **Iron Price**: Deploy cards one tier higher by paying tier as additional mana

## UI Layout Specification (Magic Online Style)

```
┌─────────────────────────────────────────────────────────────────┐
│ [Opponent Hand] [Deck] [Morale: 25] [Tier: I] [Initiative: ●]   │
├─────────────────────────────────────────────────────────────────┤
│                    [Opponent Heartland]                         │
│                     (Safe Zone)                                 │
├─────────────────────────────────────────────────────────────────┤
│ [Territory 1]     [Territory 2]     [Territory 3]              │
│ Advancing: []     Advancing: []     Advancing: []              │
│ Occupying: []     Occupying: []     Occupying: []              │
│ Settlements: []   Settlements: []   Settlements: []            │
├─────────────────────────────────────────────────────────────────┤
│                      [Your Heartland]                          │
│                       (Safe Zone)                              │
├─────────────────────────────────────────────────────────────────┤
│ [Your Hand] [Deck] [Morale: 25] [Tier: I] [Phase: Strategy]    │
│                                            [PASS BUTTON]       │
└─────────────────────────────────────────────────────────────────┘
```

## Implementation Plan

### ✅ Current Status (Updated January 2025)
- [x] ~~Project builds successfully~~ ✅ COMPLETED - All compilation errors fixed
- [x] ~~Basic models defined (Card, GameState, Player, etc.)~~ ✅ COMPLETED
- [x] ~~Deck Builder functional~~ ✅ COMPLETED
- [x] ~~Database setup (Users, UserDecks)~~ ✅ COMPLETED - Enhanced with Entity Framework
- [x] ~~Basic SignalR hub exists~~ ✅ COMPLETED - GameHub implemented
- [x] ~~Card data loading system~~ ✅ COMPLETED - Multiple card services available
- [x] ~~Authentication system~~ ✅ COMPLETED - Full JWT implementation
- [x] ~~Shared model architecture~~ ✅ COMPLETED - Proper separation between projects
- [x] ~~Password security~~ ✅ COMPLETED - BCrypt hashing implemented
- [x] ~~Database migrations~~ ✅ COMPLETED - Entity Framework configured
- [x] ~~Error handling middleware~~ ✅ COMPLETED - Comprehensive error handling

### Phase 0: Foundation Systems

#### Authentication System ✅ COMPLETED
- [x] Update User model with password hash
- [x] Password hashing service (bcrypt)
- [x] JWT token generation/validation
- [x] Login/Register pages
- [x] Authentication middleware
- [x] Session persistence

**Files Created/Modified**:
- `Empire.Server/Models/User.cs` - ✅ Added PasswordHash property
- `Empire.Server/Services/AuthenticationService.cs` - ✅ Created
- `Empire.Server/Controllers/AuthController.cs` - ✅ Created
- `Empire.Client/Pages/Login.razor` - ✅ Created
- `Empire.Client/Pages/Register.razor` - ✅ Created
- `Empire.Client/Services/AuthService.cs` - ✅ Created
- `Empire.Server/Data/EmpireDbContext.cs` - ✅ Created with SQLite
- `Empire.Server/Models/UserDeck.cs` - ✅ Created
- `Empire.Server/Services/UserService.cs` - ✅ Created

#### Enhanced Lobby System ✅ COMPLETED (Backend)
- [x] GameLobby model with player slots, spectators, settings
- [x] Lobby management service with full CRUD operations
- [x] RESTful API endpoints for lobby operations
- [x] Player slot management (Player1/Player2)
- [x] Spectator support with configurable limits
- [x] Deck validation framework (stubbed for implementation)
- [ ] Game creation interface (UI pending)
- [ ] Lobby browser UI
- [ ] Real-time lobby updates via SignalR

**Files Created/Modified**:
- `Empire.Shared/Models/GameLobby.cs` - ✅ Created with comprehensive model
- `Empire.Server/Services/LobbyService.cs` - ✅ Created with full functionality
- `Empire.Server/Controllers/LobbyController.cs` - ✅ Created with RESTful endpoints
- `Empire.Client/Pages/Lobby.razor` - ⚠️ Exists but needs major update
- `Empire.Client/Components/LobbyBrowser.razor` - 🔄 Pending
- `Empire.Client/Components/CreateGameModal.razor` - 🔄 Pending

**Implementation Notes**:
- Lobby system uses in-memory concurrent dictionary for real-time performance
- Automatic cleanup of expired lobbies every 5 minutes
- Comprehensive validation and error handling
- Ready for SignalR integration for real-time updates

### Phase 1: Core Gameplay Systems

#### Initiative & Phase Management
- [ ] Initiative tracking in GameState
- [ ] Pass action implementation
- [ ] Phase transition logic
- [ ] Visual initiative indicator
- [ ] Prominent pass button

**Files to Create/Modify**:
- `Empire.Shared/Models/GameState.cs` - Add initiative tracking
- `Empire.Server/Services/GameStateService.cs` - Add phase management
- `Empire.Server/Hubs/GameHub.cs` - Add pass action
- `Empire.Client/Components/InitiativeTracker.razor` - New
- `Empire.Client/Components/PassButton.razor` - New

#### Game Board Layout
- [ ] Magic Online style layout
- [ ] Territory components
- [ ] Heartland zones
- [ ] Drag and drop framework
- [ ] Card positioning system

**Files to Create/Modify**:
- `Empire.Client/Components/EmpireGameBoard.razor` - Major redesign
- `Empire.Client/Components/TerritoryZone.razor` - New
- `Empire.Client/Components/HeartlandZone.razor` - New
- `Empire.Client/Components/DragDropService.cs` - New
- `Empire.Client/wwwroot/css/game-board.css` - New

#### Real-time Synchronization
- [ ] Enhanced SignalR hub
- [ ] Game state broadcasting
- [ ] Player action handling
- [ ] Spectator support
- [ ] Error handling and reconnection

**Files to Create/Modify**:
- `Empire.Server/Hubs/GameHub.cs` - Major enhancement
- `Empire.Client/Services/GameHubService.cs` - Major enhancement
- `Empire.Shared/Models/GameAction.cs` - New
- `Empire.Shared/Models/GameEvent.cs` - New

### Phase 2: Game Mechanics Implementation

#### Card Actions
- [ ] Deploy Army cards
- [ ] Play villagers (once per round)
- [ ] Settle territories (once per round)
- [ ] Commit units (once per round)
- [ ] Activate abilities

#### Mana System
- [ ] Villager exertion for mana
- [ ] Tier requirements
- [ ] Iron Price rule
- [ ] Mana cost validation

#### Unit Positions
- [ ] Heartland positioning
- [ ] Advancing state
- [ ] Occupying state
- [ ] Position transitions

### Phase 3: Combat & Victory

#### Combat Resolution
- [ ] Simultaneous territory combat
- [ ] Damage assignment interface
- [ ] Excess damage to Morale
- [ ] Unit survival logic
- [ ] Territory control updates

#### Win Conditions
- [ ] Morale tracking
- [ ] Game end detection
- [ ] Victory/defeat handling
- [ ] Game result recording

### Phase 4: Polish & Features

#### Spectator Experience
- [ ] Spectator-only game view
- [ ] Chat system
- [ ] Replay functionality

#### User Experience
- [ ] Game statistics
- [ ] Match history
- [ ] Improved animations
- [ ] Mobile responsiveness

## Technical Architecture

### Database Schema
```sql
Users: Id, Username, PasswordHash, CreatedDate
UserDecks: Id, UserId, DeckName, ArmyCards, CivicCards, CreatedDate
GameLobbies: Id, Name, HostId, Player1Id, Player2Id, SpectatorIds, Status, CreatedDate
Games: Id, LobbyId, GameState, CurrentPhase, InitiativeHolder, CreatedDate
```

### SignalR Hub Methods
```csharp
// Lobby Management
CreateGame(string gameName, int maxSpectators)
JoinGame(string gameId, string deckName)
SpectateGame(string gameId)
StartGame(string gameId)

// Game Actions
PassInitiative(string gameId)
DeployCard(string gameId, int cardId, string targetZone)
PlayVillager(string gameId, int cardId)
SettleTerritory(string gameId, int cardId, string territoryId)
CommitUnits(string gameId, List<UnitCommitment> commitments)
ActivateAbility(string gameId, int cardId, string abilityId)
```

### Authentication Flow
1. User registers/logs in
2. JWT token stored in browser
3. Token included in API requests
4. SignalR connection authenticated with token
5. User identity available throughout application

## Priority Implementation Order

1. **Authentication System** (Foundation)
2. **Enhanced Lobby System** (Player matching)
3. **Initiative & Pass System** (Core gameplay)
4. **Game Board Layout** (UI foundation)
5. **Card Actions** (Basic gameplay)
6. **Combat System** (Complete game loop)
7. **Polish & Features** (User experience)

## Success Criteria

### Phase 0 Complete
- Users can register/login
- Players can create/join lobbies
- Deck validation enforced
- Spectators can join games

### Phase 1 Complete
- Initiative passes correctly
- Phase transitions work
- Magic Online style layout
- Real-time synchronization

### Phase 2 Complete
- All card actions implemented
- Mana system functional
- Unit positioning works
- Game rules enforced

### Phase 3 Complete
- Combat resolution works
- Games can be won/lost
- Full game loop functional

### Phase 4 Complete
- Spectator experience polished
- Statistics and history
- Mobile responsive
- Production ready

## 🔍 Current Analysis Findings (January 2025)

### ✅ What's Working
- **Build System**: Project compiles successfully with only minor warnings
- **Authentication**: Complete JWT-based system with secure token handling
- **Database**: SQLite with Entity Framework Core properly configured
- **Lobby Backend**: Comprehensive lobby management system implemented
- **Card System**: Multiple card services exist with JSON data loading
- **Deck Builder**: Functional deck building interface

### ❗ Critical Gaps Identified

#### 1. SignalR Implementation Incomplete
- **Current**: GameHub exists but lacks lobby integration
- **Missing**: Real-time lobby updates, player join/leave notifications
- **Impact**: Players can't see live lobby changes
- **Priority**: HIGH - Required for multiplayer experience

#### 2. Lobby UI Missing
- **Current**: Basic Lobby.razor exists but needs complete rewrite
- **Missing**: Lobby browser, create game modal, player slots UI
- **Impact**: Users can't interact with lobby system
- **Priority**: HIGH - Blocks user testing

#### 3. Game Flow Disconnected
- **Current**: Lobby system creates games but no transition to gameplay
- **Missing**: Integration between lobby start and game initialization
- **Impact**: Games can't actually begin
- **Priority**: HIGH - Core functionality gap

#### 4. Deck Validation Stubbed
- **Current**: Deck validation framework exists but not implemented
- **Missing**: Actual validation logic, card legality checks
- **Impact**: Invalid decks can enter games
- **Priority**: MEDIUM - Game integrity issue

#### 5. Card Data Source Unclear
- **Current**: Multiple card services (JsonCardDataService, CardService, MockCardDataService)
- **Missing**: Clear primary data source and loading strategy
- **Impact**: Potential data inconsistencies
- **Priority**: MEDIUM - Architecture clarity needed

### 🚧 Technical Debt
- Nullable reference warnings throughout codebase
- Unused exception variables in error handlers
- CSS keyframes syntax issues in Razor components (fixed)
- Async methods without await operators
- Unused events and fields

### 📋 Immediate Next Steps (Priority Order)

#### 🔥 CRITICAL - Blocking User Experience
1. **Create Lobby UI Components** - Enable user interaction with lobby system
   - [ ] Rewrite `Empire.Client/Pages/Lobby.razor` with modern UI
   - [ ] Implement `Empire.Client/Components/LobbyBrowser.razor` 
   - [ ] Complete `Empire.Client/Components/CreateGameModal.razor`
   - [ ] Add lobby list refresh functionality
   - [ ] Player slot management UI

2. **Integrate SignalR with Lobbies** - Real-time lobby updates
   - [ ] Enhance `Empire.Server/Hubs/GameHub.cs` with lobby methods
   - [ ] Update `Empire.Client/Services/GameHubService.cs` for lobby events
   - [ ] Add real-time lobby list updates
   - [ ] Implement player join/leave notifications
   - [ ] Add lobby status change broadcasts

3. **Connect Game Transition** - Bridge lobby start to game initialization
   - [ ] Create game start workflow in LobbyService
   - [ ] Implement game state initialization from lobby
   - [ ] Add navigation from lobby to game page
   - [ ] Ensure proper cleanup of lobby when game starts
   - [ ] Handle game start validation (both players ready, valid decks)

#### 🚨 HIGH PRIORITY - Core Functionality
4. **Implement Deck Validation** - Ensure game integrity
   - [ ] Complete deck validation logic in `Empire.Server/Services/DeckService.cs`
   - [ ] Add card legality checks (30 Army + 15 Civic)
   - [ ] Implement tier restrictions validation
   - [ ] Add duplicate card limit enforcement
   - [ ] Create validation error messaging

5. **Clarify Card Data Architecture** - Consolidate card loading strategy
   - [ ] Choose primary card service (recommend JsonCardDataService)
   - [ ] Remove or repurpose MockCardDataService
   - [ ] Standardize card loading across all components
   - [ ] Implement card caching strategy
   - [ ] Add card data validation on startup

#### 🔧 MEDIUM PRIORITY - Technical Debt
6. **Fix Compilation Warnings** - Clean up codebase
   - [ ] Address nullable reference warnings (CS8618)
   - [ ] Remove unused exception variables
   - [ ] Fix async methods without await operators
   - [ ] Remove unused events and fields
   - [ ] Add proper null checks throughout

7. **Enhance Error Handling** - Improve user experience
   - [ ] Add comprehensive try-catch blocks in services
   - [ ] Implement user-friendly error messages
   - [ ] Add logging for debugging
   - [ ] Create error boundary components
   - [ ] Add network error handling for SignalR

#### 🎯 NEW TASKS DISCOVERED
8. **Authentication Integration** - Connect auth to game flow
   - [ ] Add user authentication to lobby system
   - [ ] Implement user identity in SignalR connections
   - [ ] Add user profile display in lobbies
   - [ ] Ensure authenticated users only can create/join games
   - [ ] Add logout functionality throughout app

9. **Navigation & Routing** - Improve app flow
   - [ ] Update navigation menu with proper auth state
   - [ ] Add protected routes for authenticated pages
   - [ ] Implement proper page transitions
   - [ ] Add breadcrumb navigation
   - [ ] Handle deep linking to game pages

10. **Database Optimization** - Prepare for production
    - [ ] Add database indexes for performance
    - [ ] Implement proper connection string management
    - [ ] Add database migration scripts
    - [ ] Consider PostgreSQL for production deployment
    - [ ] Add database health checks

This roadmap serves as our master reference throughout development. Each phase builds on the previous one, ensuring we maintain a working application at each stage.
