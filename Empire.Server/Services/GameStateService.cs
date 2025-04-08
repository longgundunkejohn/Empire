using Empire.Shared.Models;
using Empire.Server.Interfaces;

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
            GameState.PlayerDecks[playerId] = new PlayerDeck(civicDeck, militaryDeck);
            GameState.PlayerHands[playerId] = new List<int>();
            GameState.PlayerBoard[playerId] = new List<BoardCard>();
            GameState.PlayerGraveyards[playerId] = new List<int>();
            GameState.PlayerLifeTotals[playerId] = 25;

            Console.WriteLine($"Initialized single-player deck for {playerId}. Civic: {civicDeck.Count}, Military: {militaryDeck.Count}");
        }



        public void DrawCard(string player, bool isCivic)
        {
            Console.WriteLine($"Attempting to draw a card for {player}");

            // Delegate drawing to the CardService
            Card? drawnCard = _cardService.DrawCard();

            if (drawnCard != null)
            {
                GameState.PlayerHands[player].Add(drawnCard.CardId);
                Console.WriteLine($"{player} drew card {drawnCard.CardId} from {(isCivic ? "Civic" : "Military")} deck.");
            }
            else
            {
                Console.WriteLine($"WARNING: {player}'s {(isCivic ? "Civic" : "Military")} deck is empty.");
            }
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