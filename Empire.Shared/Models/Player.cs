using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Empire.Shared.Models
{
    public class Player
    {
        public string Name { get; private set; }
        public int Life { get; set; }  // GameStateService modifies this
        public int MaxHandSize { get; private set; }
        public Deck Deck { get; private set; }
        public List<Card> Hand { get; private set; }
        public List<Card> DiscardPile { get; private set; }
        public List<Card> SealedAway { get; private set; }

        public Player(string name, Deck deck, int startingLife = 20, int maxHandSize = 7)
        {
            Name = name;
            Deck = deck;
            Life = startingLife;
            MaxHandSize = maxHandSize;
            Hand = new List<Card>();
            DiscardPile = new List<Card>();
            SealedAway = new List<Card>();
        }
    }
}
