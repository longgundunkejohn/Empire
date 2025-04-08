using Empire.Server.Services;
using Empire.Shared.DTOs;
using Empire.Shared.Models;
using Empire.Shared.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json; // Add this for JsonSerializerOptions

namespace Empire.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly GameSessionService _sessionService;
        private readonly GameStateService _gameStateService;
        private readonly DeckService _deckService;

        public GameController(
            GameSessionService sessionService,
            GameStateService gameStateService,
            DeckService deckService)
        {
            _sessionService = sessionService;
            _gameStateService = gameStateService;
            _deckService = deckService;
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

            _gameStateService.InitializeGame(request.Player1, playerDeck.CivicDeck, playerDeck.MilitaryDeck);

            var gameId = await _sessionService.CreateGameSession(request.Player1, new List<RawDeckEntry>());
            return Ok(gameId);
        }


        [HttpPost("join/{gameId}/{playerId}")]
        public async Task<IActionResult> JoinGame(string gameId, string playerId)
        {
            var deck = await _deckService.GetDeckAsync(playerId);

            if (deck == null || deck.Count == 0)
                return BadRequest("No deck found for this player.");

            var existingState = await _sessionService.GetGameState(gameId);
            if (existingState == null)
                return NotFound("Game not found.");

            _gameStateService.InitializeGame(
                playerId,
                deck.Where(d => d.DeckType == "Civic").Select(d => d.CardId).ToList(),
                deck.Where(d => d.DeckType == "Military").Select(d => d.CardId).ToList());

            await _sessionService.JoinGame(gameId, playerId, deck);

            return Ok(gameId);
        }
    }
}