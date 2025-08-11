# Empire Card Game - Deck Builder System

## Overview

I've created a modern, user-friendly deck builder system for your Empire Card Game that replaces the need for manual CSV file creation. This system provides a visual interface for building decks and integrates seamlessly with your existing MongoDB backend.

## What's Been Added

### 1. **Deck Builder Page** (`/deckbuilder`)
- **Visual card browser** with search and filtering
- **Drag-and-drop style deck building** (click to add cards)
- **Real-time deck validation** (30 Army cards max, 15 Civic cards max)
- **Export to CSV** functionality for compatibility
- **Save directly to MongoDB** (no CSV needed)

### 2. **API Endpoints** (`DeckBuilderController`)
- `GET /api/deckbuilder/cards` - Retrieve all available cards
- `POST /api/deckbuilder/save` - Save a deck to MongoDB
- `GET /api/deckbuilder/player/{playerName}` - Load a player's deck

### 3. **Navigation Integration**
- Added "Deck Builder" link to the main navigation menu
- Accessible from anywhere in the application

## Features

### **Card Management**
- **Automatic card type detection** (Civic cards: IDs ending in 80-99, Army cards: all others)
- **Faction-based filtering** (Amali, Kyrushima, Hjordict, Ndembe, Ohotec, Neutral)
- **Card type filtering** (Unit, Tactic, Settlement, Villager)
- **Cost-based filtering** (1-5+ cost)
- **Search by card name**

### **Deck Building**
- **Visual progress bars** showing deck completion
- **Card count tracking** (e.g., "3x Conscript")
- **Easy card removal** with minus buttons
- **Deck validation** prevents overfilling

### **Data Integration**
- **Fallback system**: Uses sample cards if MongoDB is empty
- **Compatible with existing CSV system**
- **Maintains your current card ID numbering scheme**

## Sample Data

The system includes sample cards based on what I observed from your external deck builder:

### Army Cards (1001-1079)
- Conscript, Knight of Songdu, Amali Archer
- Kyrushima Samurai, Hjordict Berserker
- Ndembe Spearman, Ohotec Scout
- Battle tactics and neutral units

### Civic Cards (1080-1099)
- Consecrated Paladin, High Priestess Stella
- Market Square, Temple of Light
- Villagers: Wise Elder, Blacksmith, Farmer
- Settlements: Monastery

## Usage

### **For Players:**
1. Navigate to `/deckbuilder`
2. Enter your player name and deck name
3. Browse and filter cards
4. Click "Add" to add cards to your deck
5. Click "Save Deck" to store in MongoDB
6. Optionally export to CSV for sharing

### **For Development:**
1. **Populate MongoDB** with real card data from your external deck builder
2. **Replace sample cards** with actual card database
3. **Add card images** to match your existing image system
4. **Extend filtering** with additional card properties

## Benefits Over CSV System

✅ **No more manual CSV creation**  
✅ **Visual deck building experience**  
✅ **Real-time validation and feedback**  
✅ **Search and filter capabilities**  
✅ **Multiple deck support per player**  
✅ **Export compatibility maintained**  
✅ **Direct MongoDB integration**  

## Next Steps

1. **Populate your MongoDB** with the complete card database from https://empire-deckbuilder.onrender.com/
2. **Test the deck builder** with your existing game flow
3. **Add card images** to enhance the visual experience
4. **Consider adding deck templates** for new players
5. **Implement deck sharing** features if desired

## Files Modified/Created

- `Empire.Client/Pages/DeckBuilder.razor` - Main deck builder page
- `Empire.Server/Controllers/DeckBuilderController.cs` - API endpoints
- `Empire.Client/Components/NavMenu.razor` - Added navigation link
- `sample-deck.csv` - Example CSV format for reference

The system is designed to work alongside your existing CSV upload system, so players can choose their preferred method for deck building.
