# 🏛️ EMPIRE CONQUEST ROADMAP TO PERFECTION 🏛️

**Mission:** Transform Empire TCG into a 100% complete Cockatrice-style manual play client
**Current Status:** 68% Complete → Target: 100% Complete
**Conquest Points Earned:** 130/1000 🏆

---

## 🎯 CONQUEST PROGRESS TRACKER

### Overall Completion: 65% ████████████████████████████████████████████████████████████████░░░░░░░░░░░░░░░░░░░░

**Phase 1: CRITICAL DEPLOYMENT FIXES** - 0% ░░░░░░░░░░░░░░░░░░░░ (200 Conquest Points)
**Phase 2: CORE EMPIRE MECHANICS** - 40% ████████░░░░░░░░░░░░ (300 Conquest Points)  
**Phase 3: COCKATRICE PARITY** - 70% ██████████████░░░░░░ (300 Conquest Points)
**Phase 4: POLISH & PERFECTION** - 20% ████░░░░░░░░░░░░░░░░ (200 Conquest Points)

---

## 🚨 PHASE 1: CRITICAL DEPLOYMENT FIXES (P0 - BLOCKING)
*Must complete before any other work. Server won't deploy without these.*

### 🔥 IMMEDIATE BLOCKERS (0/3 Complete)

- [x] **[P0-001]** ✅ Create Missing GamePreview DTO Class
  - **Issue:** `AppJsonContext.cs` references non-existent `GamePreview` class
  - **Location:** `Empire.Shared/Models/DTOs/GamePreview.cs`
  - **Conquest Points:** 50 ✅ EARNED
  - **Spec:** DTO for lobby game previews with GameId, PlayerCount, GameName, Status

- [x] **[P0-002]** ✅ Fix AppJsonContext Serialization References
  - **Issue:** Remove or fix all missing type references in JSON context
  - **Location:** `Empire.Shared/Serialization/AppJsonContext.cs`
  - **Conquest Points:** 30 ✅ EARNED
  - **Spec:** Ensure all referenced types exist and are properly imported

- [x] **[P0-003]** ✅ Verify and Fix All Missing Dependencies
  - **Issue:** Scan for other missing classes/interfaces that could break build
  - **Location:** Full codebase scan
  - **Conquest Points:** 20 ✅ EARNED
  - **Spec:** Complete dependency audit and fix all compilation errors

### 🛠️ DEPLOYMENT INFRASTRUCTURE (1/3 Complete)

- [x] **[P0-004]** ✅ Test Local Build Process
  - **Conquest Points:** 30 ✅ EARNED
  - **Spec:** Ensure `dotnet build` succeeds locally before VPS deployment

- [ ] **[P0-005]** Verify Docker Build Process
  - **Conquest Points:** 40
  - **Spec:** Test full Docker build pipeline locally

- [ ] **[P0-006]** Deploy to VPS and Verify
  - **Conquest Points:** 30
  - **Spec:** Successful deployment with working login/lobby/game creation

**Phase 1 Completion Milestone:** ✅ Server deploys successfully, basic functionality works

---

## ⚔️ PHASE 2: CORE EMPIRE MECHANICS (P1 - HIGH PRIORITY)
*Essential Empire TCG rules that make the game actually playable according to the rules PDF*

### 🎲 GAME SETUP & INITIALIZATION (2/5 Complete)

- [x] **[P1-001]** ✅ Deck Validation (30 Army + 15 Civic)
- [x] **[P1-002]** ✅ Starting Morale (25 per player)
- [ ] **[P1-003]** Opening Hand Draw (4 Army + 3 Civic)
  - **Conquest Points:** 40
  - **Spec:** Proper initial hand distribution from separate decks

- [ ] **[P1-004]** Mulligan System Implementation
  - **Conquest Points:** 60
  - **Spec:** Allow players to mulligan any number of cards once, shuffle back into respective decks

- [ ] **[P1-005]** Random Initiative Assignment
  - **Conquest Points:** 30
  - **Spec:** Randomly determine starting player, give Initiative Tracker

### 🏰 TERRITORY & TIER SYSTEM (3/6 Complete)

- [x] **[P1-006]** ✅ Three Territory Structure
- [x] **[P1-007]** ✅ Territory Occupation Tracking
- [x] **[P1-008]** ✅ Tier Calculation (I + settled territories)
- [ ] **[P1-009]** Settlement Placement Rules
  - **Conquest Points:** 50
  - **Spec:** Can only settle territory you're occupying, once per round

- [ ] **[P1-010]** Iron Price Implementation
  - **Conquest Points:** 70
  - **Spec:** Deploy cards one tier higher by paying tier as additional mana cost

- [ ] **[P1-011]** Mana Cost Calculation
  - **Conquest Points:** 60
  - **Spec:** Proper mana costs based on card tier requirements and player tier

### ⚡ CARD STATES & EXERTION (1/4 Complete)

- [x] **[P1-012]** ✅ Basic Exertion Toggle
- [ ] **[P1-013]** Exertion Rules Enforcement
  - **Conquest Points:** 80
  - **Spec:** Exerted units cannot commit, deal damage, or activate maneuvers

- [ ] **[P1-014]** Unit Position States (Heartland/Advancing/Occupying)
  - **Conquest Points:** 90
  - **Spec:** Proper tracking of unit positions and movement rules

- [ ] **[P1-015]** Commit Action Implementation
  - **Conquest Points:** 70
  - **Spec:** Move unexerted units from heartland to territories, once per round

### ⚔️ COMBAT SYSTEM (0/5 Complete)

- [ ] **[P1-016]** Damage Assignment Interface
  - **Conquest Points:** 80
  - **Spec:** Players manually assign damage to enemy units in each territory

- [ ] **[P1-017]** Excess Damage to Morale
  - **Conquest Points:** 60
  - **Spec:** Unassigned damage goes to opponent's morale if occupying territory

- [ ] **[P1-018]** Simultaneous Combat Resolution
  - **Conquest Points:** 50
  - **Spec:** Combat happens simultaneously in all three territories

- [ ] **[P1-019]** Unit Death and Graveyard
  - **Conquest Points:** 40
  - **Spec:** Units with damage ≥ defense go to graveyard

- [ ] **[P1-020]** Post-Combat Occupation
  - **Conquest Points:** 60
  - **Spec:** Surviving unopposed units can occupy territory

### 🔄 PHASE MANAGEMENT (2/4 Complete)

- [x] **[P1-021]** ✅ Three Phase Structure (Strategy/Battle/Replenishment)
- [x] **[P1-022]** ✅ Initiative Passing System
- [ ] **[P1-023]** Phase Transition Rules
  - **Conquest Points:** 50
  - **Spec:** Both players must pass to advance phase

- [ ] **[P1-024]** Replenishment Phase Actions
  - **Conquest Points:** 70
  - **Spec:** Unexert cards, draw cards (1 Army OR 2 Civic), resolve effects

**Phase 2 Completion Milestone:** ✅ Game follows Empire TCG rules accurately

---

## 🎮 PHASE 3: COCKATRICE PARITY (P1 - HIGH PRIORITY)
*Manual play features that match Cockatrice functionality*

### 🖱️ CARD INTERACTION SYSTEM (4/6 Complete)

- [x] **[P1-025]** ✅ Drag & Drop Card Movement
- [x] **[P1-026]** ✅ Click/Double-Click Actions
- [x] **[P1-027]** ✅ Card Preview on Hover
- [x] **[P1-028]** ✅ Manual Card Exertion Toggle
- [ ] **[P1-029]** Right-Click Context Menus
  - **Conquest Points:** 60
  - **Spec:** Right-click cards for action menu (exert, move to zone, etc.)

- [ ] **[P1-030]** Card Targeting System
  - **Conquest Points:** 80
  - **Spec:** Click to target for cards that require targets

### 💬 COMMUNICATION SYSTEM (3/5 Complete)

- [x] **[P1-031]** ✅ Chat System
- [x] **[P1-032]** ✅ Basic Chat Commands (/pass, /draw, etc.)
- [x] **[P1-033]** ✅ Action Logging
- [ ] **[P1-034]** Advanced Chat Commands
  - **Conquest Points:** 40
  - **Spec:** /shuffle, /peek, /reveal, /counter, /damage commands

- [ ] **[P1-035]** Game State Announcements
  - **Conquest Points:** 30
  - **Spec:** Automatic announcements for phase changes, card plays, etc.

### 🎲 MANUAL GAME CONTROLS (2/6 Complete)

- [x] **[P1-036]** ✅ Manual Draw from Decks
- [x] **[P1-037]** ✅ Manual Phase Progression
- [ ] **[P1-038]** Deck Shuffling Controls
  - **Conquest Points:** 30
  - **Spec:** Manual shuffle buttons for both decks

- [ ] **[P1-039]** Life/Morale Adjustment Controls
  - **Conquest Points:** 40
  - **Spec:** +/- buttons for manual morale adjustment

- [ ] **[P1-040]** Card Search and Reveal
  - **Conquest Points:** 50
  - **Spec:** Search deck, reveal cards, show to opponent

- [ ] **[P1-041]** Undo/Redo System
  - **Conquest Points:** 70
  - **Spec:** Limited undo for accidental actions (with opponent approval)

### 🎯 GAME STATE MANAGEMENT (3/4 Complete)

- [x] **[P1-042]** ✅ Real-time Synchronization
- [x] **[P1-043]** ✅ Game State Persistence
- [x] **[P1-044]** ✅ Reconnection Handling
- [ ] **[P1-045]** Game Save/Load System
  - **Conquest Points:** 60
  - **Spec:** Save game state, reload later

**Phase 3 Completion Milestone:** ✅ Full Cockatrice-style manual play experience

---

## ✨ PHASE 4: POLISH & PERFECTION (P2 - MEDIUM PRIORITY)
*UI/UX improvements and advanced features*

### 🎨 USER INTERFACE POLISH (1/6 Complete)

- [x] **[P2-001]** ✅ Empire-themed Styling
- [ ] **[P2-002]** Responsive Design Improvements
  - **Conquest Points:** 40
  - **Spec:** Mobile-friendly layout, tablet support

- [ ] **[P2-003]** Animation and Visual Effects
  - **Conquest Points:** 50
  - **Spec:** Card movement animations, hover effects, transitions

- [ ] **[P2-004]** Accessibility Features
  - **Conquest Points:** 30
  - **Spec:** Keyboard navigation, screen reader support

- [ ] **[P2-005]** Theme Customization
  - **Conquest Points:** 40
  - **Spec:** Multiple visual themes, dark/light mode

- [ ] **[P2-006]** Advanced Card Display
  - **Conquest Points:** 50
  - **Spec:** Card zoom, detailed tooltips, card database browser

### 🔧 ADVANCED FEATURES (0/5 Complete)

- [ ] **[P2-007]** Spectator Mode
  - **Conquest Points:** 60
  - **Spec:** Allow observers to watch games

- [ ] **[P2-008]** Replay System
  - **Conquest Points:** 70
  - **Spec:** Record and replay games

- [ ] **[P2-009]** Tournament Support
  - **Conquest Points:** 80
  - **Spec:** Bracket management, tournament lobbies

- [ ] **[P2-010]** Statistics Tracking
  - **Conquest Points:** 50
  - **Spec:** Win/loss records, deck performance

- [ ] **[P2-011]** Deck Import/Export
  - **Conquest Points:** 40
  - **Spec:** Standard deck format import/export

### 🚀 PERFORMANCE & OPTIMIZATION (0/4 Complete)

- [ ] **[P2-012]** Client Performance Optimization
  - **Conquest Points:** 40
  - **Spec:** Optimize rendering, reduce memory usage

- [ ] **[P2-013]** Server Performance Optimization
  - **Conquest Points:** 50
  - **Spec:** Database optimization, caching, load balancing

- [ ] **[P2-014]** Network Optimization
  - **Conquest Points:** 30
  - **Spec:** Reduce bandwidth usage, improve latency

- [ ] **[P2-015]** Error Handling & Recovery
  - **Conquest Points:** 40
  - **Spec:** Graceful error handling, automatic recovery

**Phase 4 Completion Milestone:** ✅ Professional-grade TCG client ready for public release

---

## 🏆 CONQUEST MILESTONES & REWARDS

### 🥉 Bronze Conquest (200 Points) - "DEPLOYMENT MASTER"
- **Reward:** Server successfully deployed and accessible
- **Unlock:** Phase 2 development access

### 🥈 Silver Conquest (500 Points) - "EMPIRE ARCHITECT" 
- **Reward:** Core Empire mechanics fully implemented
- **Unlock:** Phase 3 development access

### 🥇 Gold Conquest (800 Points) - "COCKATRICE SLAYER"
- **Reward:** Full Cockatrice parity achieved
- **Unlock:** Phase 4 development access

### 💎 Diamond Conquest (1000 Points) - "EMPIRE PERFECTION"
- **Reward:** 100% Complete Empire TCG Client
- **Achievement:** Master of the Empire Realm

---

## 📋 CURRENT SPRINT PLAN

### 🎯 NEXT ACTIONS (Priority Order):
1. **[P0-001]** Create GamePreview DTO class
2. **[P0-002]** Fix AppJsonContext references  
3. **[P0-003]** Complete dependency audit
4. **[P0-004]** Test local build
5. **[P0-005]** Test Docker build
6. **[P0-006]** Deploy to VPS

### 🔄 ITERATION STRATEGY:
- Focus on one task at a time
- Update completion status immediately
- Earn Conquest Points for motivation
- Always work within this roadmap
- Celebrate milestones achieved

---

## 📊 COMPLETION TRACKING

**Last Updated:** 2025-01-12 12:04 PM
**Current Phase:** Phase 1 - Critical Deployment Fixes
**Next Milestone:** Bronze Conquest (200 Points)
**Estimated Completion:** Phase 1 (1-2 days), Phase 2 (1 week), Phase 3 (1 week), Phase 4 (2 weeks)

---

*"The Empire shall rise, one conquest point at a time! 🏛️⚔️"*
