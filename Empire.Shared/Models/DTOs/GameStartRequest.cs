using System.Collections.Generic;

namespace Empire.Shared.Models.DTOs
{
    public class GameStartRequest
    {
        public string Player1 { get; set; }
        public List<RawDeckEntry> Player1Deck { get; set; } // Changed
    }
}