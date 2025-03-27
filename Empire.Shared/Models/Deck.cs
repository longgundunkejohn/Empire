using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Empire.Shared.Models
{
    public class Deck
    {
        private readonly List<Card> _cards;
        private static readonly Random _rng = new();
        public IEnumerable<Card> Army => _cards.Where(c => !IsCivic(c));
        public IEnumerable<Card> Civic => _cards.Where(IsCivic);
        public IEnumerable<Card> Sideboard => Enumerable.Empty<Card>(); // placeholder

        private bool IsCivic(Card card) =>
            card.Type.Equals("villager", StringComparison.OrdinalIgnoreCase) ||
            card.Type.Equals("settlement", StringComparison.OrdinalIgnoreCase);


        public Deck(IEnumerable<Card> initialCards)
        {
            if (initialCards == null)
                throw new ArgumentNullException(nameof(initialCards));

            _cards = new List<Card>(initialCards); // Defensive copy
            Shuffle();
        }

        public int Count => _cards.Count;

        public void Shuffle()
        {
            int n = _cards.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }

        public Card Draw()
        {
            if (_cards.Count == 0)
                throw new InvalidOperationException("The deck is empty.");

            var card = _cards[0];
            _cards.RemoveAt(0);
            return card;
        }

        public void PutOnTop(Card card)
        {
            if (card == null)
                throw new ArgumentNullException(nameof(card));

            _cards.Insert(0, card);
        }

        public void PutOnBottom(Card card)
        {
            if (card == null)
                throw new ArgumentNullException(nameof(card));

            _cards.Add(card);
        }

        public IReadOnlyList<Card> PeekTop(int count, bool draw = false)
        {
            if (count <= 0)
                return new List<Card>();

            if (count > _cards.Count)
                count = _cards.Count;

            var topCards = _cards.Take(count).ToList();

            if (draw)
                _cards.RemoveRange(0, count);

            return topCards;
        }

        public IReadOnlyList<Card> GetAllCards() => _cards.AsReadOnly();
    }
}
