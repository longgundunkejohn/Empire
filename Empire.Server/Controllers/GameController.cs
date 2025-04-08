using Empire.Server.Services;
using Empire.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Empire.Shared.DTOs;
using Empire.Shared.Models.DTOs;
using CsvHelper;
using System.Globalization;
using Empire.Server.Parsing;
using System.Collections.Generic;
using System.Linq;

namespace Empire.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly ICardDatabaseService _cardDatabase;
        private readonly ICardService _cardService;
        private readonly GameSessionService _sessionService;
        private readonly CardFactory _cardFactory;
        private readonly GameStateService _gameStateService;

        public GameController(
            GameSessionService sessionService,
            CardFactory cardFactory,
            ICardService cardService,
            ICardDatabaseService cardDatabase,
            GameStateService gameStateService)
        {
            _sessionService = sessionService;
            _cardFactory = cardFactory;
            _cardService = cardService;
            _cardDatabase = cardDatabase;
            _gameStateService = gameStateService;
        }

        // (GetPlayerCards, GetOpenGames, GetGameState, SubmitMove, DebugRaw remain similar)

        [HttpPost("create")]
        public async Task<ActionResult<string>> CreateGame([FromBody] GameStartRequest request, [FromServices] IServiceProvider serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(request.Player1))
                return BadRequest("Player1 is required.");

            // Get deck data from the request
            List<RawDeckEntry> player1Deck = request.Player1Deck;

            using var scope = serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<CardService>>();
            var cardDatabaseService = scope.ServiceProvider.GetRequiredService<ICardDatabaseService>();

            // Convert RawDeckEntry to List<int> for CardService
            List<int> player1DeckIds = player1Deck.SelectMany(entry => Enumerable.Repeat(entry.CardId, entry.Count)).ToList();

            // Create CardService with deck IDs
            var cardService = new CardService(player1DeckIds, cardDatabaseService, logger);

            var gameId = await _sessionService.CreateGameSession(request.Player1, player1Deck);

            // Initialize GameStateService
            _gameStateService.InitializeGame(request.Player1, player1DeckIds, player1DeckIds);

            return Ok(gameId);
        }

        private static List<int> GetAllCardIds(List<int> civicDeckIds, List<int> militaryDeckIds)
        {
            var allCardIds = new List<int>();
            allCardIds.AddRange(civicDeckIds);
            allCardIds.AddRange(militaryDeckIds);
            return allCardIds;
        }

        [HttpPost("join/{gameId}/{playerId}")]
        public async Task<IActionResult> JoinGame(string gameId, string playerId, [FromBody] List<RawDeckEntry> player2Deck, [FromServices] IServiceProvider serviceProvider)
        {
            var gameState = await _sessionService.GetGameState(gameId);
            if (gameState == null)
                return NotFound("Game not found.");

            using var scope = serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<CardService>>();
            var cardDatabaseService = scope.ServiceProvider.GetRequiredService<ICardDatabaseService>();

            // Convert RawDeckEntry to List<int> for CardService
            List<int> player2DeckIds = player2Deck.SelectMany(entry => Enumerable.Repeat(entry.CardId, entry.Count)).ToList();

            // Create CardService for Player 2
            var cardService2 = new CardService(player2DeckIds, cardDatabaseService, logger);

            await _sessionService.ApplyMove(gameId, new JoinGameMove
            {
                PlayerId = playerId,
                MoveType = "JoinGame",
                PlayerDeck = new PlayerDeck() // Dummy PlayerDeck (might remove later)
            });

            _gameStateService.InitializeGame(playerId, player2DeckIds, player2DeckIds);

            await _sessionService.JoinGame(gameId, playerId, player2Deck);

            return Ok(gameId);
        }

        // (Remove uploadDeck action)
    }
}