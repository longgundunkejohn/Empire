using System.Text.Json;

namespace Empire.Shared.Models
{
    public class UserDeck
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ArmyCards { get; set; } = string.Empty; // JSON serialized list of card IDs
        public string CivicCards { get; set; } = string.Empty; // JSON serialized list of card IDs
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        // Computed properties for UI
        public List<int> ArmyCardIds => 
            string.IsNullOrEmpty(ArmyCards) ? new List<int>() : 
            JsonSerializer.Deserialize<List<int>>(ArmyCards) ?? new List<int>();
            
        public List<int> CivicCardIds => 
            string.IsNullOrEmpty(CivicCards) ? new List<int>() : 
            JsonSerializer.Deserialize<List<int>>(CivicCards) ?? new List<int>();
            
        public int ArmyCardCount => ArmyCardIds.Count;
        public int CivicCardCount => CivicCardIds.Count;
        public int TotalCardCount => ArmyCardCount + CivicCardCount;
        
        // Empire TCG validation: 30 Army + 15 Civic = 45 total
        public bool IsValid => ArmyCardCount == 30 && CivicCardCount == 15;
        
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
    }
}
