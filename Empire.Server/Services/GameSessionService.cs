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

        public async Task<string> CreateGameSession(string player1Id, List<int> civicDeck, List<int> militaryDeck)
        {
            var gameState = new GameState
            {
                GameId = Guid.NewGuid().ToString(),
                Player1 = player1Id,
                CurrentPhase = GamePhase.Strategy,
                InitiativeHolder = player1Id,
                PriorityPlayer = player1Id,
                GameBoardState = new GameBoard(),
                PlayerDecks = new Dictionary<string, PlayerDeck>
        {
            { player1Id, new PlayerDeck(civicDeck, militaryDeck) }
        },
                PlayerHands = new Dictionary<string, List<int>>
        {
            { player1Id, new List<int>() }
        },
                PlayerBoard = new Dictionary<string, List<BoardCard>>
        {
            { player1Id, new List<BoardCard>() }
        },
                PlayerGraveyards = new Dictionary<string, List<int>>
        {
            { player1Id, new List<int>() }
        },
                PlayerLifeTotals = new Dictionary<string, int>
        {
            { player1Id, 25 }
        },
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
            var gameState = await _gameCollection.Find(gs => gs.GameId == gameId).FirstOrDefaultAsync();
            if (gameState == null) return false;

            if (!ValidateMove(gameState, move)) return false;

            ProcessMove(gameState, move);

            await _gameCollection.ReplaceOneAsync(gs => gs.GameId == gameId, gameState);
            return true;
        }

        private bool ValidateMove(GameState gameState, GameMove move)
        {
            return true; // Simplified for now, custom rules can go here
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
                        if (!gameState.PlayerBoard.ContainsKey(player))
                            gameState.PlayerBoard[player] = new List<BoardCard>();

                        gameState.PlayerBoard[player].Add(new BoardCard(move.CardId.Value));
                    }
                    break;

                case "ExertCard":
                    if (move.CardId.HasValue && gameState.PlayerBoard.ContainsKey(player))
                    {
                        var card = gameState.PlayerBoard[player].FirstOrDefault(c => c.CardId == move.CardId.Value);
                        if (card != null)
                            card.IsExerted = true;
                    }
                    break;

                case "UnexertAll":
                    if (gameState.PlayerBoard.TryGetValue(player, out var board))
                    {
                        foreach (var c in board)
                            c.IsExerted = false;
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
            }

            gameState.MoveHistory.Add(move);
        }

        public async Task<List<GameState>> ListOpenGames()
        {
            var filter = Builders<GameState>.Filter.Where(gs =>
                !string.IsNullOrEmpty(gs.Player1) && string.IsNullOrEmpty(gs.Player2));

            return await _gameCollection.Find(filter).ToListAsync();
        }

        public async Task<bool> JoinGame(string gameId, string player2Id, List<int> civicDeck, List<int> militaryDeck)
        {
            var gameState = await _gameCollection.Find(gs => gs.GameId == gameId).FirstOrDefaultAsync();

            if (gameState == null || !string.IsNullOrEmpty(gameState.Player2))
                return false;

            gameState.Player2 = player2Id;
            gameState.PlayerDecks[player2Id] = new PlayerDeck(civicDeck, militaryDeck);
            gameState.PlayerHands[player2Id] = new List<int>();
            gameState.PlayerBoard[player2Id] = new List<BoardCard>();
            gameState.PlayerGraveyards[player2Id] = new List<int>();
            gameState.PlayerLifeTotals[player2Id] = 25;

            await _gameCollection.ReplaceOneAsync(gs => gs.GameId == gameId, gameState);
            return true;
        }
    }
}
