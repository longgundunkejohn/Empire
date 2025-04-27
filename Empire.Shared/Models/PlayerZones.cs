using System.Collections.Generic;

namespace Empire.Shared.Models
{
    public class PlayerZones
    {
        public List<Card> Deck { get; set; } = new();
        public List<int> Hand { get; set; } = new();
        public List<BoardCard> Board { get; set; } = new();
        public List<int> Graveyard { get; set; } = new();
    }
}
