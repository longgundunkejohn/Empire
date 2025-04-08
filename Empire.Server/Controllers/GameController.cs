using Empire.Server.Services;
using Empire.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Empire.Shared.DTOs;
using Empire.Shared.Models.DTOs;
using CsvHelper;
using System.Globalization;
using Empire.Server.Parsing;

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
        private readonly DeckLoaderService _deckLoader;
        private readonly GameStateService _gameStateService; // Added

        public GameController(
            GameSessionService sessionService,
            CardFactory cardFactory,
            DeckLoaderService deckLoader,
            ICardService cardService,
            ICardDatabaseService cardDatabase,
            GameStateService gameStateService) // Added
        {
            _sessionService = sessionService;
            _cardFactory = cardFactory;
            _deckLoader = deckLoader;
            _cardService = cardService;
            _cardDatabase = cardDatabase;
            _gameStateService = gameStateService; // Added
        }

        [HttpGet("cards/{gameId}/{playerId}")]
        public async Task<ActionResult<List<Card>>> GetPlayerCards(string gameId, string playerId)
        {
            var game = await _sessionService.GetGameState(gameId);
            if (game == null) return NotFound("Game not found.");

            var cardIds = new HashSet<int>();

            // Get card IDs from player's hand, board, and deck
            if (game.PlayerHands.TryGetValue(playerId, out var hand))
                cardIds.UnionWith(hand);

            if (game.PlayerBoard.TryGetValue(playerId, out var board))
                cardIds.UnionWith(board.Select(b => b.CardId));

            if (game.PlayerDecks.TryGetValue(playerId, out var deck))
            {
                cardIds.UnionWith(deck.CivicDeck);
                cardIds.UnionWith(deck.MilitaryDeck);
            }

            // Call GetDeckCards with the list of card IDs
            var cards = await _cardService.GetDeckCards(cardIds.ToList());
            return Ok(cards);
        }

        [HttpGet("open")]
        public async Task<ActionResult<List<GamePreview>>> GetOpenGames()
        {
            var openGames = await _sessionService.ListOpenGames();
            if (openGames == null) return Problem("Failed to retrieve open games.");

            var previews = openGames.Select(g => new GamePreview
            {
                GameId = g.GameId,
                HostPlayer = g.Player1,
                IsJoinable = string.IsNullOrEmpty(g.Player2)
            }).ToList();

            return Ok(previews);
        }

        [HttpGet("deck/{gameId}/{playerId}")]
        public async Task<ActionResult<List<Card>>> GetPlayerDeck(string gameId, string playerId)
        {
            var state = await _sessionService.GetGameState(gameId);
            if (state == null || !state.PlayerDecks.ContainsKey(playerId))
                return NotFound("Game or player not found.");

            var deckList = state.PlayerDecks[playerId].CivicDeck
                .Concat(state.PlayerDecks[playerId].MilitaryDeck)
                .GroupBy(id => id)
                .Select(g => (CardId: g.Key, Count: g.Count()))
                .ToList();

            var fullDeck = await _cardFactory.CreateDeckAsync(deckList);
            return Ok(fullDeck);
        }


        [HttpGet("state/{gameId}/{playerId}")]
        public async Task<ActionResult<GameState>> GetGameState(string gameId, string playerId)
        {
            var state = await _sessionService.GetGameState(gameId);
            if (state == null)
                return NotFound();

            if (!state.PlayerDecks.ContainsKey(playerId))
                return BadRequest($"Player {playerId} is not part of the game.");

            return Ok(state);
        }

        [HttpPost("create")]
        public async Task<ActionResult<string>> CreateGame([FromBody] GameStartRequest request, [FromServices] IServiceProvider serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(request.Player1))
                return BadRequest("Player1 is required.");

            using var scope = serviceProvider.CreateScope();
            var deckLoader = scope.ServiceProvider.GetRequiredService<DeckLoaderService>();
            var cardDatabaseService = scope.ServiceProvider.GetRequiredService<ICardDatabaseService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<CardService>>();

            string civicDeckPath = "wwwroot/decks/Player1_Civic.csv"; // Make sure these paths are correct!
            string militaryDeckPath = "wwwroot/decks/Player1_Military.csv";

            using var civicStream = new FileStream(civicDeckPath, FileMode.Open, FileAccess.Read);
            using var militaryStream = new FileStream(militaryDeckPath, FileMode.Open, FileAccess.Read);

            var (civicDeckIds, militaryDeckIds) = deckLoader.ParseDeckFromCsv(civicStream);

            // Create CardService with the loaded deck IDs and ICardDatabaseService
            var cardService = new CardService(GetAllCardIds(civicDeckIds, militaryDeckIds), cardDatabaseService, logger);

            var gameId = await _sessionService.CreateGameSession(request.Player1, civicDeckIds, militaryDeckIds);

            // Initialize the game state with the CardService
            _gameStateService.InitializeGame(request.Player1, civicDeckIds, militaryDeckIds);

            return Ok(gameId);
        }

        private static List<int> GetAllCardIds(List<int> civicDeckIds, List<int> militaryDeckIds)
        {
            var allCardIds = new List<int>();
            allCardIds.AddRange(civicDeckIds);
            allCardIds.AddRange(militaryDeckIds);
            return allCardIds;
        }

        [HttpPost("move")]
        public async Task<IActionResult> SubmitMove([FromBody] GameMove move, [FromQuery] string gameId)
        {
            var result = await _sessionService.ApplyMove(gameId, move);
            return result ? Ok() : BadRequest("Invalid move");
        }

        [HttpPost("join/{gameId}/{playerId}")]
        public async Task<IActionResult> JoinGame(string gameId, string playerId, [FromBody] PlayerDeck playerDeck, [FromServices] IServiceProvider serviceProvider)
        {
            var gameState = await _sessionService.GetGameState(gameId);
            if (gameState == null)
                return NotFound("Game not found.");

            using var scope = serviceProvider.CreateScope();
            var deckLoader = scope.ServiceProvider.GetRequiredService<DeckLoaderService>();
            var cardDatabaseService = scope.ServiceProvider.GetRequiredService<ICardDatabaseService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<CardService>>();

            string civicDeckPath = "wwwroot/decks/Player2_Civic.csv"; // Ensure correct paths
            string militaryDeckPath = "wwwroot/decks/Player2_Military.csv";

            using var civicStream = new FileStream(civicDeckPath, FileMode.Open, FileAccess.Read);
            using var militaryStream = new FileStream(militaryDeckPath, FileMode.Open, FileAccess.Read);

            var (civicDeckIds, militaryDeckIds) = deckLoader.ParseDeckFromCsv(civicStream);

            // Create CardService for Player 2
            var cardService2 = new CardService(GetAllCardIds(civicDeckIds, militaryDeckIds), cardDatabaseService, logger);

            await _sessionService.ApplyMove(gameId, new JoinGameMove
            {
                PlayerId = playerId,
                MoveType = "JoinGame",
                PlayerDeck = playerDeck
            });

            _gameStateService.InitializeGame(playerId, civicDeckIds, militaryDeckIds);

            return Ok(gameId);
        }

        [HttpPost("debugjson")]
        public async Task<IActionResult> DebugRaw()
        {
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync();
            Console.WriteLine($"[DEBUG JSON BODY] {rawBody}");
            return Ok("Got it");
        }

        [HttpPost("uploadDeck/{gameId}")]
        public async Task<IActionResult> UploadDeck(string gameId, [FromForm] IFormFile deckCsv, [FromForm] string playerName)
        {
            try
            {
                using var stream = deckCsv.OpenReadStream();
                var (civic, military) = _deckLoader.ParseDeckFromCsv(stream);

                var playerDeck = new PlayerDeck(civic, military);
                var validIds = _cardDatabase.GetAllCards().Select(c => c.CardID).ToHashSet();
                var allDeckIds = civic.Concat(military);

                var invalid = allDeckIds.Where(id => !validIds.Contains(id)).Distinct().ToList();
                if (invalid.Any())
                    return BadRequest($"Deck contains invalid card IDs: {string.Join(", ", invalid)}");

                var game = await _sessionService.GetGameState(gameId);
                if (game == null)
                    return NotFound("Game not found.");

                game.PlayerDecks[playerName] = playerDeck;
                Console.WriteLine($"Uploaded Deck for {playerName}: CivicDeck: {string.Join(", ", civic)}, MilitaryDeck: {string.Join(", ", military)}");

                await _sessionService.ApplyMove(gameId, new JoinGameMove
                {
                    PlayerId = playerName,
                    MoveType = "JoinGame",
                    PlayerDeck = playerDeck
                });


                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UploadDeck] ERROR: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, "Deck upload failed: " + ex.Message);
            }
        }
    }
}