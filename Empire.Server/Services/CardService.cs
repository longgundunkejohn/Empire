using Empire.Server.Interfaces;
using Empire.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Empire.Server.Services
{
    public class CardService : ICardService
    {
        private readonly ICardDatabaseService _cardDb;
        private readonly Deck _deck;

        private readonly List<Card> _hand = new();
        private readonly List<Card> _board = new();
        private readonly List<Card> _graveyard = new();
        private readonly List<Card> _sealedAway = new();

        private readonly ILogger<CardService> _logger;

        public CardService(
            IEnumerable<int> initialDeckIds,
            ICardDatabaseService cardDb,
            ILogger<CardService> logger)
        {
            _cardDb = cardDb;
            _logger = logger;

            var cardData = cardDb.GetAllCards().Where(cd => initialDeckIds.Contains(cd.CardID));
            var fullDeck = cardData.Select(cd => new Card
            {
                CardId = cd.CardID,
                Name = cd.Name,
                CardText = cd.CardText,
                Faction = cd.Faction,
                Type = cd.CardType,
                ImagePath = cd.ImageFileName ?? "images/Cards/placeholder.jpg"
            }).ToList();

            if (!fullDeck.Any())
            {
                _logger.LogWarning("CardService initialized with an empty deck.");
                _deck = new Deck(new List<Card>());
                return;
            }

            _deck = new Deck(fullDeck);
        }

        // ✅ Core Getters
        public async Task<List<Card>> GetDeckCards(List<int> cardIds)
        {
            var allCardData = _cardDb.GetAllCards()
                            .Where(cd => cardIds.Contains(cd.CardID))
                .ToDictionary(cd => cd.CardID, cd => cd);

            var result = new List<Card>();

            foreach (var id in cardIds)
            {
                if (allCardData.TryGetValue(id, out var cd))
                {
                    result.Add(new Card
                    {
                        CardId = cd.CardID,
                        Name = cd.Name,
                        CardText = cd.CardText,
                        Faction = cd.Faction,
                        Type = cd.CardType,
                        ImagePath = cd.ImageFileName ?? "images/Cards/placeholder.jpg",
                        IsExerted = false,
                        CurrentDamage = 0
                    });
                }
                else
                {
                    Console.WriteLine($"❌ Card ID {id} not found in DB");
                }
            }

            Console.WriteLine($"✅ Hydrated {result.Count} cards from list of {cardIds.Count} IDs");
            return await Task.FromResult(result);
        }

        public IReadOnlyList<Card> GetHand() => _hand.AsReadOnly();
        public IReadOnlyList<Card> GetBoard() => _board.AsReadOnly();
        public IReadOnlyList<Card> GetGraveyard() => _graveyard.AsReadOnly();
        public IReadOnlyList<Card> GetSealedAway() => _sealedAway.AsReadOnly();

        // ✅ Actions
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

        public void ShuffleDeck() => _deck.Shuffle();

        // ✅ Utility
        public IReadOnlyList<Card> PeekTopOfDeck(int count, bool draw = false)
            => _deck.PeekTop(count, draw);

        public void PutOnTopOfDeck(Card card) => _deck.PutOnTop(card);
        public void PutOnBottomOfDeck(Card card) => _deck.PutOnBottom(card);
    }
}