//  File: Empire.Server/Controllers/GameController.cs
using Empire.Server.Services;
using Empire.Shared.Models.DTOs;
using Empire.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Linq; // Import Linq for .Where()

namespace Empire.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly GameSessionService _sessionService;
        private readonly GameStateService _gameStateService;
        private readonly DeckService _deckService;
        private readonly CardService _cardService; // Add CardService

        public GameController(
            GameSessionService sessionService,
            GameStateService gameStateService,
            DeckService deckService,
            CardService cardService) // Inject CardService
        {
            _sessionService = sessionService;
            _gameStateService = gameStateService;
            _deckService = deckService;
            _cardService = cardService; // Initialize CardService
        }

        [HttpGet("open")]
        public async Task<ActionResult<List<GamePreview>>> GetOpenGames()
        {
            var games = await _sessionService.ListOpenGames();
            var previews = games.Select(g => new GamePreview
            {
                GameId = g.GameId,
                HostPlayer = g.Player1,
                IsJoinable = string.IsNullOrEmpty(g.Player2)
            }).ToList();

            // Serialize the result to JSON with correct casing
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Use camelCase
            };
            var json = JsonSerializer.Serialize(previews, options);

            return Content(json, "application/json");
        }

        [HttpPost("create")]
        public async Task<ActionResult<string>> CreateGame([FromBody] GameStartRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Player1))
                return BadRequest("Player1 is required.");

            var deckLoader = HttpContext.RequestServices.GetRequiredService<DeckLoaderService>();
            var playerDeck = deckLoader.LoadDeck(request.DeckOwner);

            // Initialize the game with Civic and Military decks
            _gameStateService.InitializeGame(request.Player1, playerDeck.CivicDeck, playerDeck.MilitaryDeck);

            var gameId = await _sessionService.CreateGameSession(request.Player1, new List<RawDeckEntry>());
            return Ok(gameId);
        }

        [HttpPost("join/{gameId}/{playerId}")]
        public async Task<IActionResult> JoinGame(string gameId, string playerId)
        {
            var deck = await _deckService.GetDeckAsync(playerId);

            if (deck.Count == 0) // ✅ if deck is List<T>
                return BadRequest("No deck found for this player.");

            var existingState = await _sessionService.GetGameState(gameId);
            if (existingState == null)
                return NotFound("Game not found.");

            // Fetch the full Card objects
            var fullCivicDeck = await _cardService.GetDeckCards(deck.CivicDeck);
            var fullMilitaryDeck = await _cardService.GetDeckCards(deck.MilitaryDeck);

            // Populate the PlayerDeck.Cards property
            deck.Cards.AddRange(fullCivicDeck);
            deck.Cards.AddRange(fullMilitaryDeck);

            // Initialize the game with the player's deck
            _gameStateService.InitializeGame(playerId, deck.CivicDeck, deck.MilitaryDeck); //  Adjust GameStateService if needed

            //  IMPORTANT QUESTION:
            //  What does _sessionService.JoinGame expect as the third parameter?
            //  Currently, it's List<RawDeckEntry>.  Does it need to be changed to List<Card>?
            //  I will proceed assuming it needs to be changed to List<Card> for now.
            await _sessionService.JoinGame(gameId, playerId, deck.Cards);

            return Ok(gameId);
        }
    }
}