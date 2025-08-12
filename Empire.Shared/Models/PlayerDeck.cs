using System.Text.Json.Serialization;

namespace Empire.Shared.Models
{
    public class PlayerDeck
    {
        public string PlayerId { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public List<Card> ArmyCards { get; set; } = new();
        public List<Card> CivicCards { get; set; } = new();
        
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
        
        // Create from UserDeck
        public static PlayerDeck FromUserDeck(UserDeck userDeck, List<Card> allCards, string playerId, string playerName)
        {
            var playerDeck = new PlayerDeck
            {
                PlayerId = playerId,
                PlayerName = playerName
            };
            
            // Convert card IDs to Card objects
            var armyCardIds = userDeck.ArmyCardIds;
            var civicCardIds = userDeck.CivicCardIds;
            
            playerDeck.ArmyCards = allCards.Where(c => armyCardIds.Contains(c.Id)).ToList();
            playerDeck.CivicCards = allCards.Where(c => civicCardIds.Contains(c.Id)).ToList();
            
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
