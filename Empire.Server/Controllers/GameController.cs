using Empire.Shared.Models;
using Empire.Shared.DTOs;
using Empire.Server.Interfaces;
using Empire.Server.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Threading.Tasks;

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

            return Ok(new GamePreview
            {
                GameId = game.GameId,
                HostPlayer = request.Player1,
                IsJoinable = true
            });
        }
    }
}
