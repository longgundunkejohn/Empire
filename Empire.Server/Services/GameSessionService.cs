using Empire.Shared.Models;
using Empire.Shared.Models.DTOs;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;

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

        public async Task<string> CreateGameSession(string player1Id, List<RawDeckEntry> player1Deck)
        {
            var gameState = new GameState
            {
                GameId = Guid.NewGuid().ToString(),
                Player1 = player1Id,
                CurrentPhase = GamePhase.Strategy,
                InitiativeHolder = player1Id,
                PriorityPlayer = player1Id,
                GameBoardState = new GameBoard(),
                PlayerDecks = new Dictionary<string, PlayerDeck>(),
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

            //  Store the deck in the PlayerDecks dictionary
            gameState.PlayerDecks[player1Id] = ConvertRawDeckToPlayerDeck(player1Deck);

            Console.WriteLine($"[CreateGame] Received name: '{player1Id}', deck: {player1Deck.Count} entries");

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
                case "JoinGame":
                    if (!gameState.PlayerDecks.ContainsKey(player))
                    {
                        var joinMove = move as JoinGameMove;
                        if (joinMove?.PlayerDeck != null)
                        {
                            gameState.PlayerDecks[player] = joinMove.PlayerDeck;
                            gameState.PlayerHands[player] = new List<int>();
                            gameState.PlayerBoard[player] = new List<BoardCard>();
                            gameState.PlayerGraveyards[player] = new List<int>();
                            gameState.PlayerLifeTotals[player] = 25;

                            // Error CS1061: 'GameState' does not contain a definition for 'Player2Deck'
                            // This property needs to be added to the GameState model.
                            // gameState.Player2Deck = joinMove.PlayerDeck.CivicDeck
                            //     .Concat(joinMove.PlayerDeck.MilitaryDeck)
                            //     .GroupBy(cardId => cardId) // Group by cardId (int)
                            //     .Select(group => new RawDeckEntry { CardId = group.Key, Count = group.Count() }) // Use group.Key
                            //     .ToList();
                        }
                    }
                    break;

                    // (Rest of ProcessMove remains similar)
            }

            gameState.MoveHistory.Add(move);
        }

        public async Task<List<GameState>> ListOpenGames()
        {
            try
            {
                var filter = Builders<GameState>.Filter.Where(gs =>
                    !string.IsNullOrEmpty(gs.Player1) && string.IsNullOrEmpty(gs.Player2));

                var results = await _gameCollection.Find(filter).ToListAsync();

                Console.WriteLine($"[ListOpenGames] Found {results.Count} open games.");

                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ListOpenGames] Error: {ex.Message}");
                return new List<GameState>();
            }
        }

        public async Task<bool> JoinGame(string gameId, string player2Id, List<Card> player2Deck)
        {
            var gameState = await _gameCollection.Find(gs => gs.GameId == gameId).FirstOrDefaultAsync();

            if (gameState == null || !string.IsNullOrEmpty(gameState.Player2))
                return false;

            gameState.Player2 = player2Id;
            gameState.PlayerDecks[player2Id] = ConvertRawDeckToPlayerDeck(player2Deck); // Store Player 2's deck
            gameState.PlayerHands[player2Id] = new List<int>();
            gameState.PlayerBoard[player2Id] = new List<BoardCard>();
            gameState.PlayerGraveyards[player2Id] = new List<int>();
            gameState.PlayerLifeTotals[player2Id] = 25;

            await _gameCollection.ReplaceOneAsync(gs => gs.GameId == gameId, gameState);
            return true;
        }

        private PlayerDeck ConvertRawDeckToPlayerDeck(List<RawDeckEntry> rawDeck)
        {
            return new PlayerDeck
            {
                CivicDeck = rawDeck.Where(r => r.DeckType == "Civic").SelectMany(r => Enumerable.Repeat(r.CardId, r.Count)).ToList(),
                MilitaryDeck = rawDeck.Where(r => r.DeckType == "Military").SelectMany(r => Enumerable.Repeat(r.CardId, r.Count)).ToList()
            };
        }
    }
}