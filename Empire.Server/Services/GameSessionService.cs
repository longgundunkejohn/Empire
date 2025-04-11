using Empire.Server.Interfaces;
using Empire.Shared.Models;
using Empire.Shared.Models.DTOs;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;

namespace Empire.Server.Services
{
    public class GameSessionService
    {
        private readonly ILogger<GameSessionService> _logger;

        private readonly IMongoCollection<GameState> _gameCollection;
        private readonly CardFactory _cardFactory;

        public GameState? GameState { get; private set; }

        public GameSessionService(IMongoDatabase database, CardFactory cardFactory, ILogger<GameSessionService> logger)
        {
            _gameCollection = database.GetCollection<GameState>("GameSessions");
            _cardFactory = cardFactory;
            _logger = logger;
        }


        public async Task<string> CreateGameSession(string player1Id, List<RawDeckEntry> player1Deck)
        {
            var fullDeck = await HydrateDeckFromRawAsync(player1Deck);

            var gameState = new GameState
            {
                GameId = Guid.NewGuid().ToString(),
                Player1 = player1Id,
                Player2 = null,
                InitiativeHolder = Random.Shared.Next(2) == 0 ? player1Id : null,
                PriorityPlayer = null,
                CurrentPhase = GamePhase.Strategy,
                GameBoardState = new GameBoard(),
                PlayerDecks = new Dictionary<string, List<Card>> { { player1Id, fullDeck } },
                PlayerHands = new Dictionary<string, List<int>> { { player1Id, new List<int>() } },
                PlayerBoard = new Dictionary<string, List<BoardCard>> { { player1Id, new List<BoardCard>() } },
                PlayerGraveyards = new Dictionary<string, List<int>> { { player1Id, new List<int>() } },
                PlayerLifeTotals = new Dictionary<string, int> { { player1Id, 25 } },
                MoveHistory = new List<GameMove>()
            };

            Console.WriteLine($"[CreateGame] Received name: '{player1Id}', deck: {player1Deck.Count} entries");

            await _gameCollection.InsertOneAsync(gameState);
            return gameState.GameId;
        }
        public async Task SaveGameState(string gameId, GameState state)
        {
            await _gameCollection.ReplaceOneAsync(gs => gs.GameId == gameId, state);
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

            await ProcessMoveAsync(gameState, move);
            await _gameCollection.ReplaceOneAsync(gs => gs.GameId == gameId, gameState);
            return true;
        }

        private bool ValidateMove(GameState gameState, GameMove move) => true;

        private async Task ProcessMoveAsync(GameState gameState, GameMove move)
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
                            var fullDeck = await HydrateDeckFromIdsAsync(joinMove.PlayerDeck.CivicDeck, joinMove.PlayerDeck.MilitaryDeck);
                            gameState.PlayerDecks[player] = fullDeck;
                            gameState.PlayerHands[player] = new List<int>();
                            gameState.PlayerBoard[player] = new List<BoardCard>();
                            gameState.PlayerGraveyards[player] = new List<int>();
                            gameState.PlayerLifeTotals[player] = 25;

                            if (string.IsNullOrEmpty(gameState.InitiativeHolder))
                            {
                                gameState.InitiativeHolder = Random.Shared.Next(2) == 0 ? gameState.Player1 : player;
                            }

                            gameState.Player2 = player;
                        }
                    }
                    break;
            }

            gameState.MoveHistory.Add(move);
        }

        public async Task<bool> JoinGame(string gameId, string playerId, List<Card> deck)
        {
            var gameState = await _gameCollection.Find(gs => gs.GameId == gameId).FirstOrDefaultAsync();
            if (gameState == null || !string.IsNullOrEmpty(gameState.Player2))
                return false;

            gameState.Player2 = playerId;
            gameState.PlayerDecks[playerId] = deck;
            gameState.PlayerHands[playerId] = new List<int>();
            gameState.PlayerBoard[playerId] = new List<BoardCard>();
            gameState.PlayerGraveyards[playerId] = new List<int>();
            gameState.PlayerLifeTotals[playerId] = 25;

            if (string.IsNullOrEmpty(gameState.InitiativeHolder))
            {
                gameState.InitiativeHolder = Random.Shared.Next(2) == 0 ? gameState.Player1 : playerId;
            }

            await _gameCollection.ReplaceOneAsync(gs => gs.GameId == gameId, gameState);
            return true;
        }

        public async Task<List<GameState>> ListOpenGames()
        {
            try
            {
                var filter = Builders<GameState>.Filter.Where(gs => !string.IsNullOrEmpty(gs.Player1) && string.IsNullOrEmpty(gs.Player2));
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

        private async Task<List<Card>> HydrateDeckFromRawAsync(List<RawDeckEntry> rawDeck)
        {
            foreach (var entry in rawDeck)
            {
                if (string.IsNullOrWhiteSpace(entry.DeckType))
                {
                    entry.DeckType = DeckUtils.IsCivicCard(entry.CardId) ? "Civic" : "Military";
                }
            }

            var civicEntries = rawDeck
                .Where(d => d.DeckType == "Civic")
                .Select(d => (d.CardId, d.Count)).ToList();

            var militaryEntries = rawDeck
                .Where(d => d.DeckType == "Military")
                .Select(d => (d.CardId, d.Count)).ToList();

            var civicCards = await _cardFactory.CreateDeckAsync(civicEntries, "Civic");
            var militaryCards = await _cardFactory.CreateDeckAsync(militaryEntries, "Military");

            _logger.LogInformation("🎴 Hydrated deck: {Civic} civic / {Military} military cards",
                civicCards.Count, militaryCards.Count);

            return civicCards.Concat(militaryCards).ToList();
        }




        private async Task<List<Card>> HydrateDeckFromIdsAsync(List<int> civicIds, List<int> militaryIds)
        {
            var allIds = civicIds.Concat(militaryIds).ToList();
            var grouped = allIds.GroupBy(id => id).Select(g => (g.Key, g.Count())).ToList();
            var civicGrouped = civicIds.GroupBy(id => id).Select(g => (g.Key, g.Count())).ToList();
            var militaryGrouped = militaryIds.GroupBy(id => id).Select(g => (g.Key, g.Count())).ToList();

            var civicCards = await _cardFactory.CreateDeckAsync(civicGrouped, "Civic");
            var militaryCards = await _cardFactory.CreateDeckAsync(militaryGrouped, "Military");

            return civicCards.Concat(militaryCards).ToList();

        }


    }
}
