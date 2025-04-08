using Empire.Shared.Models;
using Empire.Server.Interfaces; // Add this

namespace Empire.Server.Services
{
    public class GameStateService
    {
        private readonly ICardService _cardService; // Changed
        private readonly Random _rng = new();

        public GameState GameState { get; private set; }

        public GameStateService(ICardService cardService) // Changed
        {
            _cardService = cardService; // Changed
            GameState = new GameState();
        }

        public void InitializeGame(string player, List<int> civicDeckIds, List<int> militaryDeckIds)
        {
            Console.WriteLine($"Initializing game for {player}...");

            // The CardService now handles the full card objects
            // No need to store IDs in GameState anymore, CardService does that

            GameState.PlayerDecks[player] = new PlayerDeck(civicDeckIds, militaryDeckIds); // Store Ids for now

            Console.WriteLine($"Initialized decks for {player}.");
        }

        public void DrawCard(string player, bool isCivic)
        {
            Console.WriteLine($"Attempting to draw a card for {player}");

            // Delegate drawing to the CardService
            Card? drawnCard = _cardService.DrawCard(); // Assuming DrawCard returns a Card or null

            if (drawnCard != null)
            {
                GameState.PlayerHands[player].Add(drawnCard.CardId); // Store CardId for now
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


        private int ExtractCardId(string filename)
        {
            string[] parts = filename.Split(' ');
            if (int.TryParse(parts[0], out int cardId))
            {
                return cardId;
            }
            throw new Exception($"Invalid card format: {filename}");
        }
    }
}