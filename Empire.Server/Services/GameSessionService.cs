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
        private readonly IMongoCollection<GameState> _gameCollection;
        private readonly CardFactory _cardFactory;

        public GameState? GameState { get; private set; }

        public GameSessionService(IMongoDatabase database, CardFactory cardFactory)
        {
            _gameCollection = database.GetCollection<GameState>("GameSessions");
            _cardFactory = cardFactory;
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

        private bool ValidateMove(GameState gameState, GameMove move) => true;

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
                            var fullDeck = ConvertDeckIntsToCards(joinMove.PlayerDeck.CivicDeck, joinMove.PlayerDeck.MilitaryDeck);
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
            var hydratedDeck = new List<Card>();

            foreach (var entry in rawDeck)
            {
                for (int i = 0; i < entry.Count; i++)
                {
                    var card = await _cardFactory.CreateCardFromIdAsync(entry.CardId);
                    if (card != null)
                        hydratedDeck.Add(card);
                }
            }

            return hydratedDeck;
        }

        private List<Card> ConvertDeckIntsToCards(List<int> civicIds, List<int> militaryIds)
        {
            var civicCards = civicIds.Select(id => _cardFactory.CreateCardFromIdAsync(id).Result).Where(card => card != null).ToList();
            var militaryCards = militaryIds.Select(id => _cardFactory.CreateCardFromIdAsync(id).Result).Where(card => card != null).ToList();
            return civicCards.Concat(militaryCards).ToList()!;
        }

        private string InferDeckType(Card card)
        {
            return card.Type.ToLower() switch
            {
                "villager" => "Civic",
                "settlement" => "Civic",
                _ => "Military"
            };
        }
    }
}
