using Empire.Shared.Models;
using Empire.Server.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Empire.Server.Services
{
    public class GameStateService
    {
        private readonly ICardService _cardService;
        private readonly DeckLoaderService _deckLoaderService;
        private readonly Random _rng = new();

        public GameState GameState { get; private set; }

        public GameStateService(ICardService cardService, DeckLoaderService deckLoaderService)
        {
            _cardService = cardService;
            _deckLoaderService = deckLoaderService;
            GameState = new GameState();
        }

        public void InitializeGame(string playerId, List<int> civicDeck, List<int> militaryDeck)
        {
            List<Card> cards = new List<Card>();
            foreach (int cardId in civicDeck)
            {
                cards.Add(new Card { CardId = cardId, Type = "Civic" });
            }
            foreach (int cardId in militaryDeck)
            {
                cards.Add(new Card { CardId = cardId, Type = "Military" });
            }

            GameState.PlayerDecks[playerId] = cards;
            GameState.PlayerHands[playerId] = new List<int>();
            GameState.PlayerBoard[playerId] = new List<BoardCard>();
            GameState.PlayerGraveyards[playerId] = new List<int>();
            GameState.PlayerLifeTotals[playerId] = 25;

            Console.WriteLine($"Initialized single-player deck for {playerId}. Civic: {civicDeck.Count}, Military: {militaryDeck.Count}");
        }

        public void DrawCard(string playerId, bool isCivic)
        {
            var currentDeck = GetDeckObject(GameState, playerId);

            // pick the subset
            var subset = isCivic ? currentDeck.Civic.ToList() : currentDeck.Army.ToList();
            if (_cardService.GetHand().Count == 0)
            {
                Console.WriteLine($"⚠️ No {(isCivic ? "civic" : "military")} cards left to draw.");
                return;
            }

            var drawnCard = subset.First(); // top card
            GameState.PlayerHands[playerId].Add(drawnCard.CardId);

            // Now update PlayerDecks with the new deck order
            var updatedDeck = currentDeck.GetAllCards().Where(c => c.CardId != drawnCard.CardId).ToList();
            GameState.PlayerDecks[playerId] = updatedDeck;

            Console.WriteLine($"✅ {playerId} drew card {drawnCard.CardId}");
        }
        private Deck GetDeckObject(GameState gameState, string playerId)
        {
            if (!gameState.PlayerDecks.TryGetValue(playerId, out var cards))
                throw new InvalidOperationException($"No deck found for player {playerId}");

            return new Deck(cards); // Your Deck class handles shuffle/draw/etc.
        }


        public void PlayCard(string player, int cardId)
        {
            // Delegate playing to CardService
            bool cardPlayed = _cardService.PlayCard(_cardService.GetHand().FirstOrDefault(c => c.CardId == cardId));

            if (cardPlayed)
            {
                GameState.PlayerBoard[player].Add(new BoardCard(cardId));
            }
        }
    }
}