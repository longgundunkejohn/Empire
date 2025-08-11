using Empire.Shared.Models;

namespace Empire.Shared.Models
{
    public class GameState
    {
        public string GameId { get; set; } = string.Empty;

        // Player Information
        public string Player1 { get; set; } = string.Empty;
        public string? Player2 { get; set; }
        
        // Empire-specific Game State
        public Dictionary<string, int> PlayerMorale { get; set; } = new(); // 25 → 0 (win condition)
        public Dictionary<string, int> PlayerTiers { get; set; } = new(); // I-IV (1-4) based on settled territories
        public int CurrentRound { get; set; } = 1;
        
        // Initiative System (Empire-specific dual priority)
        public string? InitiativeHolder { get; set; } // Passed after each round
        public string? ActionPriorityHolder { get; set; } // Passed after each action
        public bool WaitingForBothPlayersToPass { get; set; } = false;
        public string? LastPlayerToPass { get; set; }
        
        // Manual Play State
        public bool GameStarted { get; set; } = false;
        public DateTime? LastActionTime { get; set; }
        public List<string> ActionLog { get; set; } = new(); // Simple action history
        
        // Phase Management
        public GamePhase CurrentPhase { get; set; } = GamePhase.Strategy;
        
        // Action Tracking
        public Dictionary<string, List<string>> PlayerActionsThisRound { get; set; } = new(); // Track actions per player per round
        
        // Territory System (3 territories)
        public Dictionary<string, string> TerritoryOccupants { get; set; } = new(); // "territory-1" → "player1"
        public Dictionary<string, List<int>> TerritorySettlements { get; set; } = new(); // settled civic cards
        public Dictionary<string, List<int>> TerritoryAdvancingUnits { get; set; } = new(); // units advancing into territory
        public Dictionary<string, List<int>> TerritoryOccupyingUnits { get; set; } = new(); // units currently occupying territory
        
        // Player Zones
        public Dictionary<string, List<int>> PlayerArmyHands { get; set; } = new();
        public Dictionary<string, List<int>> PlayerCivicHands { get; set; } = new();
        public Dictionary<string, List<int>> PlayerHeartlands { get; set; } = new(); // Safe units
        public Dictionary<string, List<int>> PlayerVillagers { get; set; } = new(); // Villagers in heartland
        public Dictionary<string, List<Card>> PlayerArmyDecks { get; set; } = new();
        public Dictionary<string, List<Card>> PlayerCivicDecks { get; set; } = new();
        public Dictionary<string, List<int>> PlayerGraveyards { get; set; } = new();
        
        // Legacy/Compatibility (keeping for now to avoid breaking existing code)
        public Dictionary<string, List<int>> PlayerSealedZones { get; set; } = new();
        public string? PriorityPlayer { get; set; }
        public GameBoard GameBoardState { get; set; } = new GameBoard();
        public Dictionary<string, List<BoardCard>> PlayerBoard { get; set; } = new();
        public Dictionary<string, List<int>> PlayerHands { get; set; } = new();
        public Dictionary<string, List<Card>> PlayerDecks { get; set; } = new();
        public List<GameMove> MoveHistory { get; set; } = new();
        public Dictionary<string, int> PlayerLifeTotals { get; set; } = new(); // Will migrate to PlayerMorale
        
        // Helper Methods
        public void InitializePlayer(string playerId)
        {
            PlayerMorale[playerId] = 25; // Starting morale
            PlayerTiers[playerId] = 1; // Start at Tier I
            PlayerArmyHands[playerId] = new List<int>();
            PlayerCivicHands[playerId] = new List<int>();
            PlayerHeartlands[playerId] = new List<int>();
            PlayerVillagers[playerId] = new List<int>();
            PlayerArmyDecks[playerId] = new List<Card>();
            PlayerCivicDecks[playerId] = new List<Card>();
            PlayerGraveyards[playerId] = new List<int>();
            
            // Initialize territories if first player
            if (TerritoryOccupants.Count == 0)
            {
                TerritoryOccupants["territory-1"] = "";
                TerritoryOccupants["territory-2"] = "";
                TerritoryOccupants["territory-3"] = "";
                TerritorySettlements["territory-1"] = new List<int>();
                TerritorySettlements["territory-2"] = new List<int>();
                TerritorySettlements["territory-3"] = new List<int>();
                TerritoryAdvancingUnits["territory-1"] = new List<int>();
                TerritoryAdvancingUnits["territory-2"] = new List<int>();
                TerritoryAdvancingUnits["territory-3"] = new List<int>();
                TerritoryOccupyingUnits["territory-1"] = new List<int>();
                TerritoryOccupyingUnits["territory-2"] = new List<int>();
                TerritoryOccupyingUnits["territory-3"] = new List<int>();
            }
        }
        
        public int GetPlayerSettledTerritories(string playerId)
        {
            return TerritoryOccupants.Values.Count(occupant => occupant == playerId);
        }
        
        public void UpdatePlayerTier(string playerId)
        {
            PlayerTiers[playerId] = 1 + GetPlayerSettledTerritories(playerId); // Tier I + settled territories
        }
    }
}
