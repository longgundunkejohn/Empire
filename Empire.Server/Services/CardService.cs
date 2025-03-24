using Empire.Server.Services;
using Empire.Shared.Models;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Empire.Server.Services
{

    public class CardService : ICardService
    {
        private readonly CardFactory _cardFactory;

        private List<Card> _deck = new();
        private List<Card> _hand = new();
        private List<Card> _graveyard = new();
        private List<Card> _board = new();

        public CardService(CardFactory cardFactory)
        {
            _cardFactory = cardFactory;

            // Temporary: hardcoded test deck
            var testDeck = new List<(int CardId, int Count)>
        {
            (101, 2), // Knights of Songdu
            (102, 2), // Consecrated Paladin
            (103, 1)  // High Priestess N’Thalla
        };

            _deck = _cardFactory.CreateDeck(testDeck);
            ShuffleDeck();
        }

        public List<Card> GetDeck() => _deck;
        public List<Card> GetHand() => _hand;
        public List<Card> GetGraveyard() => _graveyard;
        public List<Card> GetBoard() => _board;

        public void ShuffleDeck()
        {
            var rng = new Random();
            _deck = _deck.OrderBy(_ => rng.Next()).ToList();
        }

        public Card? DrawCard()
        {
            if (_deck.Count == 0) return null;

            var drawnCard = _deck[0];
            _deck.RemoveAt(0);
            _hand.Add(drawnCard);
            return drawnCard;
        }

        public void PlayCard(Card card)
        {
            if (_hand.Contains(card))
            {
                _hand.Remove(card);
                _board.Add(card);
            }
        }

        public void MoveToGraveyard(Card card)
        {
            if (_board.Contains(card))
            {
                _board.Remove(card);
                _graveyard.Add(card);
            }
            else if (_hand.Contains(card))
            {
                _hand.Remove(card);
                _graveyard.Add(card);
            }
        }
    }
}
