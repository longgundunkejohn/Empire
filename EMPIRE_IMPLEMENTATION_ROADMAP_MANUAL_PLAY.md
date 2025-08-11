# Empire TCG Implementation Roadmap - Manual Play System
*Updated: January 8, 2025*

## 🎯 Current Status: MANUAL PLAY BACKEND COMPLETE

### ✅ COMPLETED PHASES

#### Phase 1: Foundation & Authentication ✅
- [x] User registration and login system
- [x] JWT token authentication
- [x] Database setup with Entity Framework
- [x] Basic project structure (Client/Server/Shared)

#### Phase 2: Deck Building System ✅
- [x] Card data management (200+ cards)
- [x] Deck builder UI with drag-and-drop
- [x] CSV import functionality
- [x] Deck validation and saving
- [x] User deck management

#### Phase 3: Lobby System ✅
- [x] Game lobby creation and joining
- [x] Real-time lobby updates via SignalR
- [x] Player ready status management
- [x] Deck selection for games
- [x] Game room navigation

#### Phase 4: Manual Play Backend ✅ **JUST COMPLETED**
- [x] Enhanced GameState model with Empire-specific mechanics
- [x] Manual game action DTOs (Cockatrice-style)
- [x] Complete GameHub with manual play methods
- [x] Priority system implementation (Initiative + Action Priority)
- [x] Empire mechanics (Morale, Tiers, Territories)
- [x] Card movement and state management
- [x] Zone management (Heartland, Territories, Decks, etc.)

---

## 🚧 CURRENT PHASE: Manual Play Frontend

### Phase 5: Manual Play Frontend Implementation

#### 5.1 Enhanced Game Board UI 🔄 **IN PROGRESS**
- [ ] **Card Interaction System**
  - [ ] Drag-and-drop between zones
  - [ ] Right-click context menus
  - [ ] Double-click to tap/untap
  - [ ] Visual feedback for valid drop zones
  - [ ] Card selection and multi-select

- [ ] **Zone Components Enhancement**
  - [ ] Heartland zone with unit management
  - [ ] Territory zones (3 territories)
  - [ ] Hand zones (Army/Civic separation)
  - [ ] Deck zones with shuffle/draw actions
  - [ ] Graveyard with card viewing

- [ ] **Game State Display**
  - [ ] Morale counters (25 → 0)
  - [ ] Tier indicators (I-IV)
  - [ ] Phase/Round display
  - [ ] Initiative/Priority indicators
  - [ ] Territory occupation status

#### 5.2 Manual Play Controls 🔄 **IN PROGRESS**
- [ ] **Priority System UI**
  - [ ] Pass Priority button
  - [ ] Pass Initiative button
  - [ ] Phase advancement controls
  - [ ] Round advancement controls
  - [ ] Visual priority indicators

- [ ] **Card Action Menus**
  - [ ] Right-click context menus
  - [ ] Counter management (+1/+1, damage, escalation)
  - [ ] Flip face up/down
  - [ ] Move to specific zones
  - [ ] Batch operations (untap all)

- [ ] **Game Flow Controls**
  - [ ] Draw cards (Army/Civic choice)
  - [ ] Shuffle decks
  - [ ] Replenishment actions
  - [ ] Morale adjustment
  - [ ] Tier management

#### 5.3 SignalR Client Integration 🔄 **IN PROGRESS**
- [ ] **GameHubService Enhancement**
  - [ ] Manual play method bindings
  - [ ] Real-time game state updates
  - [ ] Action synchronization
  - [ ] Error handling and reconnection

- [ ] **Event Handling**
  - [ ] Card movement events
  - [ ] Priority passing events
  - [ ] Phase/round advancement
  - [ ] Player action notifications
  - [ ] Game state synchronization

#### 5.4 Empire-Specific UI Elements 🔄 **IN PROGRESS**
- [ ] **Territory Management**
  - [ ] Territory occupation indicators
  - [ ] Settlement placement
  - [ ] Unit advancement/retreat
  - [ ] Combat damage assignment

- [ ] **Empire Mechanics Display**
  - [ ] Exertion visual indicators
  - [ ] Tier progression display
  - [ ] Morale tracking with animations
  - [ ] Initiative tracker
  - [ ] Action history log

---

## 📋 UPCOMING PHASES

### Phase 6: Game Rules Engine (Optional Enhancement)
- [ ] **Automated Rule Validation**
  - [ ] Turn structure enforcement
  - [ ] Legal action validation
  - [ ] Card ability parsing
  - [ ] Win condition detection

- [ ] **Smart Assistance**
  - [ ] Legal move highlighting
  - [ ] Automatic phase transitions
  - [ ] Rule reminders
  - [ ] Undo/redo system

### Phase 7: Advanced Features
- [ ] **Spectator Mode**
  - [ ] Watch ongoing games
  - [ ] Replay system
  - [ ] Game recording

- [ ] **Tournament System**
  - [ ] Tournament creation
  - [ ] Bracket management
  - [ ] Swiss pairings
  - [ ] Results tracking

- [ ] **Social Features**
  - [ ] Friend lists
  - [ ] Private messaging
  - [ ] Game invitations
  - [ ] Player profiles

### Phase 8: Polish & Optimization
- [ ] **Performance Optimization**
  - [ ] Client-side caching
  - [ ] Efficient state management
  - [ ] Network optimization
  - [ ] Memory management

- [ ] **UI/UX Polish**
  - [ ] Animations and transitions
  - [ ] Sound effects
  - [ ] Accessibility features
  - [ ] Mobile responsiveness

---

## 🛠️ TECHNICAL ARCHITECTURE

### Backend (✅ Complete)
```
Empire.Server/
├── Controllers/          # API endpoints
├── Hubs/                # SignalR GameHub with manual play
├── Services/            # Business logic
├── Models/              # Data models
└── Data/                # Database context

Empire.Shared/
├── Models/              # Shared data models
│   ├── GameState.cs     # Enhanced with Empire mechanics
│   └── DTOs/            # Manual game actions
└── Enums/               # Game enumerations
```

### Frontend (🔄 In Progress)
```
Empire.Client/
├── Pages/               # Razor pages
│   ├── Game.razor       # Main game interface
│   └── GameRoom.razor   # Game lobby
├── Components/          # Reusable components
│   ├── EmpireGameBoard.razor    # Main game board
│   ├── HandComponent.razor      # Player hand
│   ├── TerritoryComponent.razor # Territory zones
│   └── CardComponent.razor      # Individual cards
└── Services/            # Client services
    ├── GameHubService.cs        # SignalR client
    └── GameStateClientService.cs # State management
```

---

## 🎮 EMPIRE TCG RULES IMPLEMENTATION

### Core Mechanics ✅ **IMPLEMENTED**
- **Initiative System**: Dual priority (Initiative + Action Priority)
- **Morale System**: 25 starting morale, 0 = defeat
- **Tier System**: I-IV based on settled territories
- **Territory System**: 3 territories with occupation/settlement
- **Card Types**: Units, Tactics, Battle Tactics, Chronicles, Villagers, Settlements

### Game Flow ✅ **BACKEND READY**
1. **Strategy Phase**: Deploy cards, play villagers, settle territories, commit units
2. **Battle Phase**: Maneuvers and combat
3. **Replenishment**: Unexert cards, draw cards, pass initiative

### Manual Play Features ✅ **BACKEND READY**
- **Card Movement**: Drag-and-drop between all zones
- **Card States**: Tap/untap, face up/down, counters
- **Priority Passing**: Action priority and initiative passing
- **Phase Management**: Manual phase and round advancement
- **Communication**: Ping system and in-game chat

---

## 🚀 IMMEDIATE NEXT STEPS

### Week 1: Core Game Board Enhancement
1. **Enhance EmpireGameBoard.razor**
   - Add drag-and-drop functionality
   - Implement zone-based card management
   - Add visual feedback for interactions

2. **Update CardComponent.razor**
   - Add right-click context menus
   - Implement tap/untap visual states
   - Add counter display

3. **Enhance GameHubService.cs**
   - Bind to new manual play methods
   - Handle real-time updates
   - Implement error handling

### Week 2: Manual Play Controls
1. **Create ManualPlayControls.razor**
   - Priority passing buttons
   - Phase advancement controls
   - Game state displays

2. **Implement Card Actions**
   - Context menu system
   - Counter management
   - Zone movement options

3. **Add Empire-Specific UI**
   - Morale counters
   - Tier indicators
   - Territory status

### Week 3: Integration & Testing
1. **Full Integration Testing**
   - End-to-end game flow
   - Multi-player synchronization
   - Error handling

2. **UI Polish**
   - Visual feedback
   - Animations
   - Responsive design

3. **Documentation**
   - User guide
   - Developer documentation
   - Deployment guide

---

## 📊 PROGRESS TRACKING

- **Overall Progress**: 65% Complete
- **Backend**: 95% Complete ✅
- **Frontend**: 35% Complete 🔄
- **Testing**: 20% Complete ⏳
- **Documentation**: 40% Complete ⏳

---

## 🎯 SUCCESS CRITERIA

### Minimum Viable Product (MVP)
- [x] User authentication and deck building
- [x] Lobby system with game creation
- [x] Manual play backend infrastructure
- [ ] **Basic manual play frontend** ← **CURRENT FOCUS**
- [ ] Card movement and basic interactions
- [ ] Priority passing system
- [ ] Game state synchronization

### Full Release
- [ ] Complete Empire TCG rule implementation
- [ ] Polished UI with animations
- [ ] Tournament system
- [ ] Spectator mode
- [ ] Mobile support

---

*This roadmap reflects the current state after implementing the complete manual play backend system. The focus now shifts to frontend implementation to create a fully playable Empire TCG experience.*
