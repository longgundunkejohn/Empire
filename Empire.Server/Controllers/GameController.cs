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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        [HttpPost("create")]
        public async Task<ActionResult<string>> CreateGame([FromBody] GameStartRequest request, [FromServices] IServiceProvider serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(request.Player1))
                return BadRequest("Player1 is required.");

            if (request.Player1Deck == null || !request.Player1Deck.Any())
                return BadRequest("Player1Deck is required and cannot be empty.");

            using var scope = serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<CardService>>();
            var cardDatabaseService = scope.ServiceProvider.GetRequiredService<ICardDatabaseService>();

            try
            {
                var player1Deck = request.Player1Deck;
                var player1DeckIds = player1Deck.SelectMany(entry => Enumerable.Repeat(entry.CardId, entry.Count)).ToList();

                var cardService = new CardService(player1DeckIds, cardDatabaseService, logger);
                var gameId = await _sessionService.CreateGameSession(request.Player1, player1Deck);

                _gameStateService.InitializeGame(
                    request.Player1,
                    player1Deck.Where(d => d.DeckType == "Civic").Select(d => d.CardId).ToList(),
                    player1Deck.Where(d => d.DeckType == "Military").Select(d => d.CardId).ToList()
                );

                return Ok(gameId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "🧨 Failed to create game for {Player}", request.Player1);
                return StatusCode(500, "Failed to create game: " + ex.Message);
            }
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

            // Error CS1501: No overload for method 'InitializeGame' takes 3 arguments
            // Check the signature of InitializeGame in GameStateService and ensure it matches the arguments being passed.
            _gameStateService.InitializeGame(playerId,
                                                player2Deck.Where(d => d.DeckType == "Civic").Select(d => d.CardId).ToList(),
                                                player2Deck.Where(d => d.DeckType == "Military").Select(d => d.CardId).ToList());

            await _sessionService.JoinGame(gameId, playerId, player2Deck);

            return Ok(gameId);
        }

        // (Remove uploadDeck action)
    }
}