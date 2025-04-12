using Empire.Server.Interfaces;
using Empire.Shared.Models;
using Empire.Shared.Models.DTOs;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            var playerDeck = ConvertRawDeckToPlayerDeck(player1Id, player1Deck);
            var fullDeck = await HydrateDeckFromPlayerDeckAsync(playerDeck);

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

        public async Task<GameState?> GetGameState(string gameId)
        {
            var state = await _gameCollection.Find(gs => gs.GameId == gameId).FirstOrDefaultAsync();
            GameState = state;
            return state;
        }

        public async Task SaveGameState(string gameId, GameState state)
        {
            await _gameCollection.ReplaceOneAsync(gs => gs.GameId == gameId, state);
        }

        private PlayerDeck ConvertRawDeckToPlayerDeck(string playerName, List<RawDeckEntry> rawDeck)
        {
            var civicDeck = new List<int>();
            var militaryDeck = new List<int>();

            foreach (var entry in rawDeck)
            {
                var type = entry.DeckType?.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(type))
                {
                    type = DeckUtils.IsCivicCard(entry.CardId) ? "civic" : "military";
                }

                for (int i = 0; i < entry.Count; i++)
                {
                    if (type == "civic") civicDeck.Add(entry.CardId);
                    else if (type == "military") militaryDeck.Add(entry.CardId);
                    else _logger.LogWarning("Unknown deck type '{DeckType}' for card ID {CardId}", entry.DeckType, entry.CardId);
                }
            }

            _logger.LogInformation("Built deck for {Player} — Civic: {CivicCount}, Military: {MilitaryCount}",
                playerName, civicDeck.Count, militaryDeck.Count);

            return new PlayerDeck(playerName, civicDeck, militaryDeck);
        }

        private async Task<List<Card>> HydrateDeckFromPlayerDeckAsync(PlayerDeck deck)
        {
            var civicCards = await _cardFactory.CreateDeckAsync(deck.CivicDeck, "Civic");
            var militaryCards = await _cardFactory.CreateDeckAsync(deck.MilitaryDeck, "Military");

            Console.WriteLine($"✅ Hydrated {civicCards.Count} civic + {militaryCards.Count} military = {civicCards.Count + militaryCards.Count} total cards");

            return civicCards.Concat(militaryCards).ToList();
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

        private async Task<List<Card>> HydrateDeckFromIdsAsync(List<int> civicIds, List<int> militaryIds)
        {
            var civicCards = await _cardFactory.CreateDeckAsync(civicIds, "Civic");
            var militaryCards = await _cardFactory.CreateDeckAsync(militaryIds, "Military");

            return civicCards.Concat(militaryCards).ToList();
        }

    }
}
