using Empire.Shared.Models;
using MongoDB.Driver;

namespace Empire.Server.Services
{
    public class GameSessionService
    {
        private readonly IMongoCollection<GameState> _gameCollection;

        public GameState? GameState { get; private set; }

        public GameSessionService(IMongoDatabase database)
        {
            _gameCollection = database.GetCollection<GameState>("GameSessions");
        }

        public async Task<string> CreateGameSession(string player1Id, string player2Id)
        {
            var gameState = new GameState
            {
                GameId = Guid.NewGuid().ToString(),
                Player1 = player1Id,
                Player2 = player2Id,
                CurrentPhase = GamePhase.Strategy,
                InitiativeHolder = player1Id,
                PriorityPlayer = player2Id,
                GameBoardState = new Empire.Shared.Models.GameBoard(),
                PlayerDecks = new Dictionary<string, PlayerDeck>
{
    { player1Id, new PlayerDeck(new List<int>(), new List<int>()) },
    { player2Id, new PlayerDeck(new List<int>(), new List<int>()) }
},

                PlayerGraveyards = new Dictionary<string, List<int>>(),

                MoveHistory = new List<GameMove>()
            };

            await _gameCollection.InsertOneAsync(gameState);
            return gameState.GameId;
        }
        public async Task<GameState?> GetGameState(string gameId)
        {
            var state = await _gameCollection.Find(gs => gs.GameId == gameId).FirstOrDefaultAsync();
            GameState = state;
            return state;
        }

        public async Task<bool> ApplyMove(string gameId, GameMove move)
        {
            // Fetch game state from MongoDB
            var gameState = await _gameCollection.Find(gs => gs.GameId == gameId).FirstOrDefaultAsync();

            if (gameState == null) return false; // Game not found

            bool isValid = ValidateMove(gameState, move);
            if (!isValid) return false; // Invalid move, no need to update DB

            // Apply the move
            ProcessMove(gameState, move);

            // Save the updated game state back to MongoDB
            await _gameCollection.ReplaceOneAsync(gs => gs.GameId == gameId, gameState);

            return true;
        }
        private bool ValidateMove(GameState gameState, GameMove move)
        {
            // Example: Check if the player is the active player
            if (gameState.PriorityPlayer != move.PlayerId) return false;

            // Example: Ensure the card exists in the player's hand
            if (move.CardId == null) return false; // ✅ Prevent null error

            if (!gameState.PlayerHands[move.PlayerId].Contains(move.CardId.Value))
            {
                return false;
            }


            // Add more game-specific validation rules here

            return true;
        }
        private void ProcessMove(GameState gameState, GameMove move)
        {
            gameState.PlayerHands[move.PlayerId].Remove(move.CardId.Value);
            gameState.GameBoardState.PlayedCards.Add(move.CardId.Value);

            // Switch priority to the next player
            gameState.PriorityPlayer = (gameState.PriorityPlayer == gameState.Player1) ? gameState.Player2 : gameState.Player1;

            // Save move history
            gameState.MoveHistory.Add(move);
        }


    }
}
