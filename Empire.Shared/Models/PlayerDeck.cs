using System.Collections.Generic;

namespace Empire.Shared.Models
{
    public class PlayerDeck
    {
        // List of card IDs for Civic Deck
        public List<int> CivicDeck { get; set; }

        // List of card IDs for Military Deck
        public List<int> MilitaryDeck { get; set; }

        // Constructor to initialize the decks
        public PlayerDeck(List<int> civicDeck, List<int> militaryDeck)
        {
            CivicDeck = civicDeck;
            MilitaryDeck = militaryDeck;
        }
    }
}
