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

        public GameController(
            GameSessionService sessionService,
            CardFactory cardFactory,
            DeckLoaderService deckLoader,
            ICardService cardService,
            ICardDatabaseService cardDatabase)
        {
            _sessionService = sessionService;
            _cardFactory = cardFactory;
            _deckLoader = deckLoader;
            _cardService = cardService;
            _cardDatabase = cardDatabase;
        }

        [HttpGet("cards/{gameId}/{playerId}")]
        public async Task<ActionResult<List<Card>>> GetPlayerCards(string gameId, string playerId)
        {
            var game = await _sessionService.GetGameState(gameId);
            if (game == null) return NotFound("Game not found.");

            var cardIds = new HashSet<int>();

            if (game.PlayerHands.TryGetValue(playerId, out var hand))
                cardIds.UnionWith(hand);

            if (game.PlayerBoard.TryGetValue(playerId, out var board))
                cardIds.UnionWith(board.Select(b => b.CardId));

            if (game.PlayerDecks.TryGetValue(playerId, out var deck))
            {
                cardIds.UnionWith(deck.CivicDeck);
                cardIds.UnionWith(deck.MilitaryDeck);
            }

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
        public async Task<ActionResult<string>> CreateGame([FromBody] GameStartRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Player1))
                return BadRequest("Player1 is required.");

            var gameId = await _sessionService.CreateGameSession(request.Player1, new List<int>(), new List<int>());
            return Ok(gameId);
        }

        [HttpPost("move")]
        public async Task<IActionResult> SubmitMove([FromBody] GameMove move, [FromQuery] string gameId)
        {
            var result = await _sessionService.ApplyMove(gameId, move);
            return result ? Ok() : BadRequest("Invalid move");
        }

        [HttpPost("join/{gameId}/{playerId}")]
        public async Task<IActionResult> JoinGame(string gameId, string playerId, [FromBody] PlayerDeck playerDeck)
        {
            var gameState = await _sessionService.GetGameState(gameId);
            if (gameState == null)
                return NotFound("Game not found.");

            await _sessionService.ApplyMove(gameId, new JoinGameMove
            {
                PlayerId = playerId,
                MoveType = "JoinGame",
                PlayerDeck = playerDeck
            });


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
