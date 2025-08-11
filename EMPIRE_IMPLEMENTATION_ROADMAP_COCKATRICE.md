# Empire TCG Implementation Roadmap - Cockatrice Manual Play Approach

## Current Status (Completed ✅)

### Phase 1: Foundation & Authentication
- ✅ User authentication system (login/register)
- ✅ Database integration with Entity Framework
- ✅ Basic project structure and dependency injection
- ✅ Authentication middleware and JWT tokens

### Phase 2: Lobby System
- ✅ Enhanced LobbyBrowser component with real-time updates
- ✅ CreateGameModal with deck validation
- ✅ Lobby page redesign with proper authentication
- ✅ Game lobby models and DTOs
- ✅ Lobby controller and service implementation
- ✅ Real-time lobby updates (10-second refresh)

### Phase 3: Deck Management
- ✅ Deck builder interface
- ✅ Card database integration
- ✅ Deck validation (30 Army + 15 Civic cards)
- ✅ User deck storage and retrieval

### Phase 4: SignalR & Real-time Communication
- ✅ Enhanced GameHubService with comprehensive event system
- ✅ Empire-specific SignalR events (initiative, card actions, combat)
- ✅ Game room SignalR support (join/leave/ready notifications)
- ✅ Server-side GameHub with full Empire TCG mechanics
- ✅ Enhanced LobbyController with game room endpoints
- ✅ Connection management and auto-reconnection
- ✅ Proper error handling and logging
- ✅ Fixed compilation errors and event handler signatures
- ✅ Removed polling timers in favor of real-time updates

### Phase 5: Game Room System
- ✅ Create `/lobby/{id}` route for specific game rooms (GameRoom.razor)
- ✅ Enhanced game room component with real-time SignalR updates
- ✅ SignalR integration for game room events
- ✅ Player join/leave notifications
- ✅ Ready state synchronization infrastructure
- ✅ LobbyService with SetPlayerReadyAsync method
- ✅ Ready System Integration
- ✅ Deck Selection Integration
- ✅ Game Room UI/UX

## NEW APPROACH: Manual Play Environment (Cockatrice-Style)

### Phase 6: Manual Play Foundation (CURRENT PRIORITY - Week 1)
**Target: Get players playing immediately with manual rule enforcement**

#### 6.1 Priority System Implementation
- [ ] Initiative Tracker (passed after each round)
- [ ] Action Priority (passed after each action)
- [ ] Visual indicators for who can act
- [ ] "Pass Priority" and "Pass Initiative" buttons
- [ ] Clear priority state in GameState model

#### 6.2 Basic Game Zones & Card Movement
- [ ] Player zones: Hand, Heartland, Graveyard, Sealed Zone
- [ ] Shared zones: 3 Territories with sub-zones
- [ ] Basic drag & drop card movement
- [ ] Right-click context menus for card actions
- [ ] Zone viewers (graveyard, sealed cards, etc.)

#### 6.3 Game State Synchronization (Manual)
- [ ] Simplified GameState model for manual play
- [ ] Real-time state updates via SignalR
- [ ] No rule validation - pure state tracking
- [ ] Game initialization from lobby

### Phase 7: Territory System & Card Interactions (Week 2)
**Target: Full manual game environment**

#### 7.1 Territory Implementation
- [ ] 3 Territory visual areas
- [ ] Sub-zones per territory:
  - Advancing units area
  - Occupying unit area
  - Settlement area
- [ ] Territory ownership indicators
- [ ] Drag & drop to/from territories

#### 7.2 Card Interaction System
- [ ] Double-click to zoom/enlarge cards
- [ ] Tap/Untap (exert/unexert) functionality
- [ ] Flip cards face up/down
- [ ] Card counters (+1/+1, damage, etc.)
- [ ] Card notes/markers system
- [ ] Card positioning within zones

#### 7.3 Enhanced Card Movement
- [ ] Smooth drag & drop animations
- [ ] Visual feedback for valid drop zones
- [ ] Batch card operations
- [ ] Undo/redo for card movements

### Phase 8: Quality of Life Features (Week 3)
**Target: Streamlined manual play experience**

#### 8.1 Batch Operations
- [ ] "Untap all my units" button
- [ ] "Draw X cards" buttons
- [ ] "Send multiple cards to graveyard"
- [ ] "Shuffle deck" functionality
- [ ] Mass card selection tools

#### 8.2 Game State Helpers
- [ ] Phase indicator (Strategy/Battle/Replenishment)
- [ ] Round counter display
- [ ] Player tier tracking (I-IV)
- [ ] Morale tracking with +/- buttons
- [ ] Turn timer (optional)

#### 8.3 Communication & Utility Tools
- [ ] In-game chat system
- [ ] Ping system (highlight cards/areas)
- [ ] "Undo request" functionality
- [ ] Simple action history log
- [ ] Game state export/import

### Phase 9: Polish & Testing (Week 4)
**Target: Production-ready manual play**

#### 9.1 UI/UX Polish
- [ ] Responsive design for different screen sizes
- [ ] Smooth animations and transitions
- [ ] Clear visual hierarchy
- [ ] Accessibility improvements
- [ ] Mobile-friendly interface

#### 9.2 Performance & Stability
- [ ] Optimize SignalR message frequency
- [ ] Efficient card rendering
- [ ] Memory leak prevention
- [ ] Connection stability improvements
- [ ] Error recovery mechanisms

#### 9.3 Player Testing & Feedback
- [ ] Beta testing with real players
- [ ] Gather feedback on manual play experience
- [ ] Identify most-needed automation features
- [ ] Performance testing under load
- [ ] Bug fixes and stability improvements

## Future Phases (Post-Manual Implementation)

### Phase 10: Selective Automation (Future)
**Target: Add automation for most common actions**
- [ ] Automated drawing at start of turn
- [ ] Automated untapping at replenishment
- [ ] Basic mana calculation helpers
- [ ] Combat damage calculation
- [ ] Win condition detection

### Phase 11: Advanced Automation (Future)
**Target: Rule enforcement for complex interactions**
- [ ] Card effect automation
- [ ] Timing and priority automation
- [ ] Complex interaction resolution
- [ ] Tournament mode with full rules
- [ ] AI opponent (far future)

### Phase 12: Advanced Features (Future)
**Target: Enhanced competitive experience**
- [ ] Spectator system
- [ ] Game replays and analysis
- [ ] Tournament brackets
- [ ] Player rankings and statistics
- [ ] Mobile app development

## Technical Architecture for Manual Play

### Core Principles
1. **Zero rule enforcement** - players handle all rules manually
2. **Maximum flexibility** - any card can go anywhere
3. **Clear game state** - all players see everything
4. **Fast interactions** - minimal clicks for common actions
5. **Undo-friendly** - easy to reverse mistakes

### Key Models
```csharp
public class ManualGameState 
{
    // Priority System
    public string InitiativeHolder { get; set; }
    public string ActionPriorityHolder { get; set; }
    
    // Game Info
    public int Round { get; set; }
    public GamePhase Phase { get; set; }
    public Dictionary<string, int> PlayerMorale { get; set; }
    public Dictionary<string, int> PlayerTiers { get; set; }
    
    // Card Zones (flexible positioning)
    public Dictionary<string, List<GameCard>> PlayerZones { get; set; }
    public Dictionary<string, List<GameCard>> TerritoryZones { get; set; }
    public Dictionary<string, List<GameCard>> SharedZones { get; set; }
}
```

### Implementation Strategy
1. **Start simple** - basic card movement first
2. **Iterate quickly** - get feedback from real players
3. **Add features incrementally** - based on actual needs
4. **Maintain flexibility** - don't lock into rigid systems
5. **Plan for automation** - design with future automation in mind

## Success Metrics

### Phase 6 Success Criteria
- Players can start games from lobby
- Basic card movement works
- Priority system functions
- Real-time updates work

### Phase 7 Success Criteria
- Full manual games can be played
- Territory system works
- All card interactions available
- Smooth drag & drop experience

### Phase 8 Success Criteria
- QoL features speed up gameplay
- Players prefer digital to physical
- Common actions are streamlined
- Communication tools work well

### Phase 9 Success Criteria
- 30+ players actively using system
- Games complete without technical issues
- Players provide positive feedback
- System ready for wider release

This roadmap prioritizes getting a playable manual environment working in 3-4 weeks, then iterating based on real player feedback. The focus is on enabling the existing community to play immediately while building toward future automation.
