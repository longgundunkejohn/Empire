using Empire.Server.Services;
using Empire.Shared.Models.DTOs;
using Empire.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Collections.Generic;
using Empire.Shared.Models;

namespace Empire.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly GameSessionService _sessionService;
        private readonly GameStateService _gameStateService;
        private readonly DeckService _deckService;
        private readonly ICardService _cardService;

        public GameController(
            GameSessionService sessionService,
            GameStateService gameStateService,
            DeckService deckService,
            ICardService cardService)
        {
            _sessionService = sessionService;
            _gameStateService = gameStateService;
            _deckService = deckService;
            _cardService = cardService;
        }

        [HttpGet("{gameId}/state")]
        public async Task<IActionResult> GetGameState(string gameId)
        {
            var state = await _sessionService.GetGameState(gameId);
            if (state == null)
                return NotFound("Game not found.");
            return Ok(state);
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

            return Ok(previews);
        }

        [HttpPost("create")]
        public async Task<ActionResult<string>> CreateGame([FromBody] GameStartRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Player1))
                return BadRequest("Player1 is required.");

            var deckLoader = HttpContext.RequestServices.GetRequiredService<DeckLoaderService>();
            var playerDeck = deckLoader.LoadDeck(request.DeckOwner);

            var fullCivicDeck = await _cardService.GetDeckCards(playerDeck.CivicDeck);
            var fullMilitaryDeck = await _cardService.GetDeckCards(playerDeck.MilitaryDeck);

            _gameStateService.InitializeGame(request.Player1, fullCivicDeck, fullMilitaryDeck);

            // 🔥 Creates an empty deck in Mongo representation but initializes the game logic
            var gameId = await _sessionService.CreateGameSession(request.Player1, new List<RawDeckEntry>());

            return Ok(gameId);  // Fixed return statement
        }

        [HttpPost("join/{gameId}/{playerId}")]
        public async Task<IActionResult> JoinGame(string gameId, string playerId)
        {
            var deck = await _deckService.GetDeckAsync(playerId);

            if (deck == null || deck.CivicDeck.Count == 0 && deck.MilitaryDeck.Count == 0)
                return BadRequest("No deck found for this player.");

            var existingState = await _sessionService.GetGameState(gameId);
            if (existingState == null)
                return NotFound("Game not found.");

            // ✅ Pull the full cards for each half of the deck
            var fullCivicDeck = await _cardService.GetDeckCards(deck.CivicDeck);
            var fullMilitaryDeck = await _cardService.GetDeckCards(deck.MilitaryDeck);

            // ✅ Combine both civic and military decks into the game state
            //deck.CivicDeck.AddRange(fullCivicDeck);
            //deck.MilitaryDeck.AddRange(fullMilitaryDeck);

            var combinedDeck = fullCivicDeck.Concat(fullMilitaryDeck).ToList();

            // ✅ Initialize game state with raw IDs
            _gameStateService.InitializeGame(playerId, fullCivicDeck, fullMilitaryDeck);

            // ✅ Join game session
            await _sessionService.JoinGame(gameId, playerId, combinedDeck);

            return Ok(gameId);
        }
    }
}