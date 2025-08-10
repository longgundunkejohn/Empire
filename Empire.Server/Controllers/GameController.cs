// 🔧 FILE: GameController.cs (Empire.Server/Controllers)
using Empire.Shared.Models;
using Empire.Shared.DTOs;
using Empire.Server.Interfaces;
using Empire.Server.Services;
using Empire.Server.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Empire.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly DeckService _deckService;
        private readonly ICardDatabaseService _cardService;
        private readonly IMongoCollection<GameState> _gameCollection;
        private readonly IHubContext<GameHub> _hubContext;

        public GameController(DeckService deckService, ICardDatabaseService cardService, IMongoDatabase mongoDb, IHubContext<GameHub> hubContext)
        {
            _deckService = deckService;
            _cardService = cardService;
            _gameCollection = mongoDb.GetCollection<GameState>("GameSessions");
            _hubContext = hubContext;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateGame([FromBody] GameStartRequest request)
        {
            var playerDeck = await _deckService.GetDeckAsync(request.Player1, request.DeckId);
            if (playerDeck == null)
                return NotFound("No deck found for this player.");

            var civicCards = await _cardService.GetDeckCards(playerDeck.CivicDeck);
            var militaryCards = await _cardService.GetDeckCards(playerDeck.MilitaryDeck);
            var allCards = civicCards.Concat(militaryCards).ToList();

            var hand = allCards.Take(5).Select(c => c.CardId).ToList();

            var game = new GameState
            {
                GameId = ObjectId.GenerateNewId().ToString(),
                Player1 = request.Player1,
                CurrentPhase = GamePhase.Strategy,
                PlayerDecks = new Dictionary<string, List<Card>> { [request.Player1] = allCards },
                PlayerHands = new Dictionary<string, List<int>> { [request.Player1] = hand },
                PlayerBoard = new Dictionary<string, List<BoardCard>> { [request.Player1] = new() },
                PlayerGraveyards = new Dictionary<string, List<int>> { [request.Player1] = new() },
                PlayerLifeTotals = new Dictionary<string, int> { [request.Player1] = 20 },
                PlayerSealedZones = new Dictionary<string, List<int>> { [request.Player1] = new() }
            };

            await _gameCollection.InsertOneAsync(game);

            return Ok(game.GameId); // Only return the GameId string!
        }

        [HttpGet("open")]
        public async Task<ActionResult<List<GamePreview>>> GetOpenGames()
        {
            var filter = Builders<GameState>.Filter.Where(g => !string.IsNullOrEmpty(g.Player1) && string.IsNullOrEmpty(g.Player2));
            var games = await _gameCollection.Find(filter).ToListAsync();

            var previews = games.Select(g => new GamePreview
            {
                GameId = g.GameId,
                HostPlayer = g.Player1,
                IsJoinable = true
            }).ToList();

            return Ok(previews);
        }

        [HttpPost("{gameId}/join/{playerId}")]
        public async Task<IActionResult> JoinGame(string gameId, string playerId, [FromBody] JoinGameRequest deck)
        {
            var game = await _gameCollection.Find(g => g.GameId == gameId).FirstOrDefaultAsync();
            if (game == null)
                return NotFound("Game not found");

            if (!string.IsNullOrEmpty(game.Player2))
                return BadRequest("Game already full");

            var civicCards = await _cardService.GetDeckCards(deck.CivicDeck);
            var militaryCards = await _cardService.GetDeckCards(deck.MilitaryDeck);
            var allCards = civicCards.Concat(militaryCards).ToList();

            var hand = allCards.Take(5).Select(c => c.CardId).ToList();

            game.Player2 = playerId;

            game.PlayerDecks[playerId] = allCards;
            game.PlayerHands[playerId] = hand;
            game.PlayerBoard[playerId] = new();
            game.PlayerGraveyards[playerId] = new();
            game.PlayerLifeTotals[playerId] = 20;
            game.PlayerSealedZones[playerId] = new();

            await _gameCollection.ReplaceOneAsync(g => g.GameId == gameId, game);

            return Ok(new { message = $"✅ {playerId} joined game {gameId}" });
        }

        [HttpPost("{gameId}/draw/{playerId}/{type}")]
        public async Task<ActionResult<int>> DrawCard(string gameId, string playerId, string type)
        {
            var game = await _gameCollection.Find(g => g.GameId == gameId).FirstOrDefaultAsync();
            if (game == null) return NotFound();

            if (!game.PlayerDecks.TryGetValue(playerId, out var deck))
                return BadRequest("Deck not found for player");

            var drawPool = deck.Where(c =>
                type.ToLower() == "civic" ? DeckUtils.IsCivicCard(c.CardId) : !DeckUtils.IsCivicCard(c.CardId)).ToList();

            if (!drawPool.Any()) return BadRequest("No cards left of type");

            var card = drawPool.First();
            deck.Remove(card);

            if (!game.PlayerHands.ContainsKey(playerId))
                game.PlayerHands[playerId] = new();

            game.PlayerHands[playerId].Add(card.CardId);

            await _gameCollection.ReplaceOneAsync(g => g.GameId == gameId, game);

            return Ok(card.CardId);
        }

        [HttpGet("{gameId}/state")]
        public async Task<ActionResult<GameState>> GetGameState(string gameId)
        {
            var state = await _gameCollection.Find(g => g.GameId == gameId).FirstOrDefaultAsync();
            return state == null ? NotFound() : Ok(state);
        }

        [HttpPost("{gameId}/move")]
        public async Task<IActionResult> SubmitMove(string gameId, [FromBody] GameMove move)
        {
            var game = await _gameCollection.Find(g => g.GameId == gameId).FirstOrDefaultAsync();
            if (game == null)
                return NotFound("Game not found");

            // Add move to history
            game.MoveHistory.Add(move);

            // Process the move based on type
            switch (move.MoveType?.ToLowerInvariant())
            {
                case "shuffledeck":
                    await ProcessShuffleDeck(game, move.PlayerId);
                    break;
                case "playcard":
                    if (move.CardId.HasValue)
                        await ProcessPlayCard(game, move.PlayerId, move.CardId.Value);
                    break;
                case "exertcard":
                    if (move.CardId.HasValue)
                        await ProcessExertCard(game, move.PlayerId, move.CardId.Value);
                    break;
                case "endturn":
                    await ProcessEndTurn(game, move.PlayerId);
                    break;
                default:
                    return BadRequest($"Unknown move type: {move.MoveType}");
            }

            // Save updated game state
            await _gameCollection.ReplaceOneAsync(g => g.GameId == gameId, game);

            // Notify clients about the move and game state update
            await _hubContext.Clients.Group(gameId).SendAsync("MoveSubmitted", move);
            await _hubContext.Clients.Group(gameId).SendAsync("GameStateUpdated", gameId);

            return Ok(new { message = $"Move {move.MoveType} processed successfully" });
        }

        private async Task ProcessShuffleDeck(GameState game, string playerId)
        {
            if (game.PlayerDecks.TryGetValue(playerId, out var deck))
            {
                // Simple shuffle using Random
                var random = new Random();
                for (int i = deck.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    (deck[i], deck[j]) = (deck[j], deck[i]);
                }
            }
            await Task.CompletedTask;
        }

        private async Task ProcessPlayCard(GameState game, string playerId, int cardId)
        {
            // Move card from hand to board
            if (game.PlayerHands.TryGetValue(playerId, out var hand) && hand.Contains(cardId))
            {
                hand.Remove(cardId);
                
                if (!game.PlayerBoard.ContainsKey(playerId))
                    game.PlayerBoard[playerId] = new List<BoardCard>();
                
                game.PlayerBoard[playerId].Add(new BoardCard(cardId));
            }
            await Task.CompletedTask;
        }

        private async Task ProcessExertCard(GameState game, string playerId, int cardId)
        {
            // Find card on board and mark as exerted
            if (game.PlayerBoard.TryGetValue(playerId, out var board))
            {
                var boardCard = board.FirstOrDefault(bc => bc.CardId == cardId);
                if (boardCard != null)
                {
                    boardCard.IsExerted = true;
                }
            }
            await Task.CompletedTask;
        }

        private async Task ProcessEndTurn(GameState game, string playerId)
        {
            // Switch active player and advance phase
            if (game.Player1 == playerId)
            {
                game.PriorityPlayer = game.Player2;
            }
            else if (game.Player2 == playerId)
            {
                game.PriorityPlayer = game.Player1;
            }

            // Advance game phase
            game.CurrentPhase = game.CurrentPhase switch
            {
                GamePhase.Strategy => GamePhase.Action,
                GamePhase.Action => GamePhase.Resolution,
                GamePhase.Resolution => GamePhase.Strategy,
                _ => GamePhase.Strategy
            };

            // Unexert all cards for the player ending their turn
            if (game.PlayerBoard.TryGetValue(playerId, out var board))
            {
                foreach (var card in board)
                {
                    card.IsExerted = false;
                }
            }

            await Task.CompletedTask;
        }
    }
}
