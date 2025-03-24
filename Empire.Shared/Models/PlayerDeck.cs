using System.Collections.Generic;

namespace Empire.Shared.Models
{
    public class PlayerDeck
    {
        public List<int> CivicDeck { get; set; }
        public List<int> MilitaryDeck { get; set; }

        public PlayerDeck(List<int> civicDeck, List<int> militaryDeck)
        {
            CivicDeck = civicDeck;
            MilitaryDeck = militaryDeck;
        }
    }
}
