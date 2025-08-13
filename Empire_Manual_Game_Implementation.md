# ?? Empire TCG Manual Game System (Cockatrice-Style)

## ?? **Overview**
We've successfully implemented a Cockatrice-like manual game system for Empire TCG that gives players full control over their cards and game state, similar to how Cockatrice works for Magic: The Gathering.

## ? **What's Implemented**

### ?? **Core Manual Game Features**
- **Player-Driven Rules**: No server-side rule enforcement - players are responsible for following the rules
- **Full Card Control**: Drag & drop, right-click context menus, double-click actions
- **Manual Zone Management**: Cards can be moved between any zones freely
- **Exertion System**: Toggle card exertion (tap/untap) with visual indicators
- **Chat Integration**: All actions logged to chat for transparency

### ?? **Key Components Created**

#### 1. **ManualGameService.cs**
- Handles all manual game operations
- No rule validation - pure player control
- Quick actions for common Empire operations
- Dice rolling, coin flipping, card revealing
- Chat command system

#### 2. **ManualCardComponent.razor**
- Enhanced card display with overlay information
- Context menu (right-click) with common actions
- Visual exertion indicators
- Faction and card type color coding
- Drag & drop support

#### 3. **GameZoneComponent.razor**
- Flexible zone system (hand, deck, battlefield, graveyard)
- Multiple layout options (spread, stack, grid, fan)
- Zone-specific actions (shuffle, draw, unexert all)
- Drop target highlighting

#### 4. **ManualGame.razor**
- Full game interface layout
- Territory system for Empire's 3-territory structure
- Real-time chat and action logging
- Tool panel with dice, coins, and shortcuts
- Card preview and selection system

### ?? **Visual Features**
- **Card Images**: Proper image loading from `/images/Cards/{CardID}.jpg`
- **Card Backs**: Fallback to appropriate card backs when images unavailable
- **Theme**: Dark Empire-style UI with gold accents
- **Responsive Design**: Works on different screen sizes
- **Animations**: Smooth hover effects and transitions

### ?? **Technical Features**
- **SignalR Integration**: Real-time communication between players
- **Drag & Drop**: Full JavaScript interop for card movement
- **Keyboard Shortcuts**: Space to pass, Ctrl+D to draw, etc.
- **Context Menus**: Right-click for quick actions
- **State Management**: Client-side game state tracking

## ?? **How to Use**

### **Accessing the Manual Game**
Navigate to: `/game-manual/demo/player1` or use the navigation menu.

### **Basic Controls**
- **Left Click**: Select card
- **Double Click**: Toggle exertion (tap/untap)
- **Right Click**: Context menu with actions
- **Drag & Drop**: Move cards between zones
- **Chat Commands**: `/draw`, `/pass`, `/unexert`, etc.

### **Empire-Specific Features**
- **Territory Control**: 3 territories with settlement zones
- **Dual Decks**: Separate Army and Civic decks/hands
- **Phase Management**: Manual phase progression
- **Morale Tracking**: Player life totals (Empire calls it "Morale")

## ?? **Empire Rules Implementation**

### **What Players Must Track Manually**
1. **Card Costs**: Players pay mana costs themselves
2. **Rule Timing**: Following phase structure and timing
3. **Once-per-Round**: Restrictions like villager playing, settling
4. **Combat Resolution**: Damage assignment and territory control
5. **Victory Conditions**: Reducing opponent to 0 morale

### **Quick Actions Available**
- Draw 1 Army card or 2 Civic cards
- Pass initiative
- Unexert all cards
- Roll dice / flip coins
- Shuffle decks
- Move cards between zones

## ?? **Future Enhancements**

### **Phase 2 Features**
- [ ] **Spectator Mode**: Allow others to watch games
- [ ] **Replay System**: Save and replay games
- [ ] **Card Database Search**: In-game card lookup
- [ ] **Deck Import**: Load decks from CSV/text files
- [ ] **Sound Effects**: Audio feedback for actions
- [ ] **Advanced Shortcuts**: More keyboard shortcuts

### **Phase 3 Features**
- [ ] **Tournament Mode**: Organized play support
- [ ] **Deck Sharing**: Share deck lists with others
- [ ] **Game Templates**: Pre-configured game states
- [ ] **Rules Reference**: In-game rules lookup
- [ ] **Card Scanner**: Mobile card recognition

## ?? **Getting Started**

1. **Navigate** to the manual game: `/game-manual/demo/player1`
2. **Learn the Interface**: Familiarize yourself with zones and controls
3. **Practice**: Try moving cards, using chat commands
4. **Play**: Start a game with a friend using the same game ID
5. **Communicate**: Use chat to coordinate and announce actions

## ?? **Benefits Over Automated Systems**

- **Flexibility**: Handle any edge case or rule interaction
- **Speed**: No waiting for server validation
- **Learning**: Forces players to learn the rules properly
- **Creativity**: Can handle house rules or variants
- **Reliability**: No bugs in rule implementation

This system provides the foundation for a Cockatrice-like experience where players have complete control over their game, making it perfect for competitive play, testing, and learning the Empire TCG rules.