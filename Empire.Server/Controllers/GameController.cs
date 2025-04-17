// 🔧 FILE: GameController.cs (Empire.Server/Controllers)
using Empire.Shared.Models;
using Empire.Shared.DTOs;
using Empire.Server.Interfaces;
using Empire.Server.Services;
using Microsoft.AspNetCore.Mvc;
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

        public GameController(DeckService deckService, ICardDatabaseService cardService, IMongoDatabase mongoDb)
        {
            _deckService = deckService;
            _cardService = cardService;
            _gameCollection = mongoDb.GetCollection<GameState>("games");
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateGame([FromBody] GameStartRequest request)
        {
            var playerDeck = await _deckService.GetDeckAsync(request.Player1);
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
                PlayerLifeTotals = new Dictionary<string, int> { [request.Player1] = 20 }
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
    }
}
