using Empire.Server.Interfaces;
using Empire.Shared.Models;

namespace Empire.Server.Services
{
    public class CardService : ICardService
    {
        private readonly Deck _deck;
        private readonly List<Card> _hand = new();
        private readonly List<Card> _board = new();
        private readonly List<Card> _graveyard = new();
        private readonly List<Card> _sealedAway = new();

        public CardService(IEnumerable<Card> initialDeck)
        {
            if (initialDeck == null || !initialDeck.Any())
                throw new ArgumentException("Initial deck must contain cards.", nameof(initialDeck));

            _deck = new Deck(initialDeck);
        }

        // Core Getters
        public IReadOnlyList<Card> GetDeckCards() => _deck.GetAllCards();
        public IReadOnlyList<Card> GetHand() => _hand.AsReadOnly();
        public IReadOnlyList<Card> GetBoard() => _board.AsReadOnly();
        public IReadOnlyList<Card> GetGraveyard() => _graveyard.AsReadOnly();
        public IReadOnlyList<Card> GetSealedAway() => _sealedAway.AsReadOnly();

        // Actions
        public Card? DrawCard()
        {
            if (_deck.Count == 0) return null;

            var card = _deck.Draw();
            _hand.Add(card);
            return card;
        }

        public bool PlayCard(Card card)
        {
            if (!_hand.Contains(card)) return false;

            _hand.Remove(card);
            _board.Add(card);
            return true;
        }

        public bool MoveToGraveyard(Card card)
        {
            if (_board.Remove(card) || _hand.Remove(card))
            {
                _graveyard.Add(card);
                return true;
            }

            return false;
        }

        public bool SealCard(Card card)
        {
            if (_board.Remove(card) || _hand.Remove(card))
            {
                _sealedAway.Add(card);
                return true;
            }

            return false;
        }

        public void ShuffleDeck()
        {
            _deck.Shuffle();
        }

        // Optional: Peek methods for UI or abilities
        public IReadOnlyList<Card> PeekTopOfDeck(int count, bool draw = false)
        {
            return _deck.PeekTop(count, draw);
        }

        // Optional: Move card back to deck
        public void PutOnTopOfDeck(Card card)
        {
            _deck.PutOnTop(card);
        }

        public void PutOnBottomOfDeck(Card card)
        {
            _deck.PutOnBottom(card);
        }
    }
}
