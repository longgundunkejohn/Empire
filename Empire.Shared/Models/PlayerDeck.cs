using System.Collections.Generic;

namespace Empire.Shared.Models
{
    public class PlayerDeck
    {
        public List<int> CivicDeck { get; set; } = new();
        public List<int> MilitaryDeck { get; set; } = new();

        // 👇 Required for Blazor + JSON deserialization + object initializer
        public PlayerDeck() { }

        public PlayerDeck(List<int> civicDeck, List<int> militaryDeck)
        {
            CivicDeck = civicDeck;
            MilitaryDeck = militaryDeck;
        }
    }
}