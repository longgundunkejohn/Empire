using System.Text.Json.Serialization;

namespace Empire.Shared.Models
{
    public class PlayerDeck
    {
        public string PlayerId { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public string DeckName { get; set; } = string.Empty;
        public List<Card> ArmyCards { get; set; } = new();
        public List<Card> CivicCards { get; set; } = new();
        
        // Legacy properties for backward compatibility
        public List<int> CivicDeck { get; set; } = new();
        public List<int> MilitaryDeck { get; set; } = new();
        
        [JsonIgnore]
        public int ArmyCardCount => ArmyCards.Count;
        
        [JsonIgnore]
        public int CivicCardCount => CivicCards.Count;
        
        [JsonIgnore]
        public int TotalCardCount => ArmyCardCount + CivicCardCount;
        
        // Empire TCG validation: 30 Army + 15 Civic = 45 total
        [JsonIgnore]
        public bool IsValid => ArmyCardCount == 30 && CivicCardCount == 15;
        
        [JsonIgnore]
        public string ValidationMessage
        {
            get
            {
                if (IsValid) return "Valid deck";
                
                var issues = new List<string>();
                if (ArmyCardCount != 30) issues.Add($"Army: {ArmyCardCount}/30");
                if (CivicCardCount != 15) issues.Add($"Civic: {CivicCardCount}/15");
                
                return string.Join(", ", issues);
            }
        }
        
        // Default constructor
        public PlayerDeck()
        {
        }
        
        // Constructor that takes 3 arguments (for backward compatibility)
        public PlayerDeck(string playerName, List<int> civicDeck, List<int> militaryDeck)
        {
            PlayerName = playerName;
            CivicDeck = civicDeck ?? new List<int>();
            MilitaryDeck = militaryDeck ?? new List<int>();
            DeckName = $"{playerName}'s Deck";
        }
        
        // Create from UserDeck
        public static PlayerDeck FromUserDeck(UserDeck userDeck, List<Card> allCards, string playerId, string playerName)
        {
            var playerDeck = new PlayerDeck
            {
                PlayerId = playerId,
                PlayerName = playerName,
                DeckName = $"{playerName}'s Deck"
            };
            
            // Convert card IDs to Card objects
            var armyCardIds = userDeck.ArmyCardIds;
            var civicCardIds = userDeck.CivicCardIds;
            
            playerDeck.ArmyCards = allCards.Where(c => armyCardIds.Contains(c.CardId)).ToList();
            playerDeck.CivicCards = allCards.Where(c => civicCardIds.Contains(c.CardId)).ToList();
            
            // Also populate legacy properties
            playerDeck.CivicDeck = civicCardIds;
            playerDeck.MilitaryDeck = armyCardIds;
            
            return playerDeck;
        }
        
        // Create shuffled runtime decks
        public (Deck armyDeck, Deck civicDeck) CreateShuffledDecks()
        {
            var armyDeck = new Deck(ArmyCards);
            var civicDeck = new Deck(CivicCards);
            
            return (armyDeck, civicDeck);
        }
    }
}
