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
            var player = move.PlayerId;

            switch (move.MoveType)
            {
                case "DrawCivicCard":
                    if (gameState.PlayerDecks[player].CivicDeck.Any())
                    {
                        var cardId = gameState.PlayerDecks[player].CivicDeck[0];
                        gameState.PlayerDecks[player].CivicDeck.RemoveAt(0);
                        gameState.PlayerHands[player].Add(cardId);
                    }
                    break;

                case "GainLife":
                    if (move.Value.HasValue)
                        gameState.PlayerLifeTotals[player] += move.Value.Value;
                    break;

                case "LoseLife":
                    if (move.Value.HasValue)
                        gameState.PlayerLifeTotals[player] -= move.Value.Value;
                    break;

                case "DrawMilitaryCard":
                    if (gameState.PlayerDecks[player].MilitaryDeck.Any())
                    {
                        var cardId = gameState.PlayerDecks[player].MilitaryDeck[0];
                        gameState.PlayerDecks[player].MilitaryDeck.RemoveAt(0);
                        gameState.PlayerHands[player].Add(cardId);
                    }
                    break;

                case "PlayCard":
                    if (move.CardId.HasValue && gameState.PlayerHands[player].Contains(move.CardId.Value))
                    {
                        gameState.PlayerHands[player].Remove(move.CardId.Value);
                        gameState.PlayerBoard[player].Add(move.CardId.Value);
                    }
                    break;

                case "MoveToGraveyard":
                    if (move.CardId.HasValue)
                    {
                        int cardId = move.CardId.Value;
                        // Remove from hand or board
                        gameState.PlayerHands[player].Remove(cardId);
                        gameState.PlayerBoard[player].Remove(cardId);

                        gameState.PlayerGraveyards[player].Add(cardId);
                    }
                    break;

                case "SealCard":
                    if (move.CardId.HasValue)
                    {
                        int cardId = move.CardId.Value;
                        gameState.PlayerHands[player].Remove(cardId);
                        gameState.PlayerBoard[player].Remove(cardId);
                        // Add sealing logic (new zone?) — we can scaffold a `PlayerSealedAway` dictionary like the others
                    }
                    break;

                    // Add more like Exert, Rotate, etc later
            }

            gameState.MoveHistory.Add(move);

            // Rotate priority
            gameState.PriorityPlayer = gameState.PriorityPlayer == gameState.Player1
                ? gameState.Player2
                : gameState.Player1;
        }
        public async Task<List<GameState>> ListOpenGames()
        {
            var filter = Builders<GameState>.Filter.Where(gs =>
                !string.IsNullOrEmpty(gs.Player1) && string.IsNullOrEmpty(gs.Player2)
            );

            var openGames = await _gameCollection.Find(filter).ToListAsync();
            return openGames;
        }
        public async Task<bool> JoinGame(string gameId, string player2Id, List<int> civicDeck, List<int> militaryDeck)
        {
            var gameState = await _gameCollection.Find(gs => gs.GameId == gameId).FirstOrDefaultAsync();

            if (gameState == null || !string.IsNullOrEmpty(gameState.Player2))
            {
                // Game does not exist or Player2 has already joined
                return false;
            }

            // Assign Player 2
            gameState.Player2 = player2Id;

            // Create Player 2's deck (same structure as Player 1's deck)
            gameState.PlayerDecks[player2Id] = new PlayerDeck(civicDeck, militaryDeck);

            // Initialize Player 2's hand and other zones
            gameState.PlayerHands[player2Id] = new List<int>();
            gameState.PlayerBoard[player2Id] = new List<int>();
            gameState.PlayerGraveyards[player2Id] = new List<int>();

            // Set Player 1 as the initiative holder, or set any other rules for turn order
            gameState.InitiativeHolder = gameState.Player1;

            // Save updated game state
            await _gameCollection.ReplaceOneAsync(gs => gs.GameId == gameId, gameState);

            return true;
        }


    }
}
