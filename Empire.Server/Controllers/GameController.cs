using Empire.Server.Services;
using Empire.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Empire.Shared.DTOs;
using Empire.Shared.Interfaces;

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
            ICardDatabaseService cardDatabase) // 🔥 add this
        {
            _sessionService = sessionService;
            _cardFactory = cardFactory;
            _deckLoader = deckLoader;
            _cardService = cardService;

            // if you also need to store/use _cardDatabase later
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
                cardIds.UnionWith(board.Select(bc => bc.CardId));

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
            if (state == null) return NotFound("Game not found.");
            if (!state.PlayerDecks.ContainsKey(playerId)) return NotFound("Player not found in game.");

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
            {
                Console.WriteLine($"[GameApi] Game not found: {gameId}");
                return NotFound();
            }

            if (!state.PlayerDecks.ContainsKey(playerId))
            {
                Console.WriteLine($"[GameApi] Player {playerId} not found in game {gameId}");
                return BadRequest($"Player {playerId} is not part of the game.");
            }

            return Ok(state);
        }

        [HttpPost("create")]
        public async Task<ActionResult<string>> CreateGame([FromForm] IFormFile deckCsv, [FromForm] string playerId)
        {
            try
            {
                if (deckCsv == null || deckCsv.Length == 0)
                    return BadRequest("CSV is required.");

                var tempPath = Path.GetTempFileName();
                using (var stream = System.IO.File.Create(tempPath))
                {
                    await deckCsv.CopyToAsync(stream);
                }

                var playerDeck = _deckLoader.LoadDeckFromSingleCSV(tempPath);
                var gameId = await _sessionService.CreateGameSession(playerId, playerDeck.CivicDeck, playerDeck.MilitaryDeck);

                return Ok(gameId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameApi] Failed to create game: {ex.Message}");
                return StatusCode(500, $"Failed to create game: {ex.Message}");
            }
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

            gameState.PlayerDecks[playerId] = playerDeck;
            await _sessionService.ApplyMove(gameId, new GameMove
            {
                PlayerId = playerId,
                MoveType = "JoinGame"
            });

            return Ok(gameId);
        }

        [HttpPost("uploadDeck/{gameId}")]
        public async Task<IActionResult> UploadDeck(string gameId, [FromForm] IFormFile deckCsv, [FromForm] string playerName)
        {
            if (deckCsv == null || deckCsv.Length == 0)
                return BadRequest("Deck file is missing");

            var tempPath = Path.GetTempFileName();
            using (var stream = System.IO.File.Create(tempPath))
            {
                await deckCsv.CopyToAsync(stream);
            }

            var playerDeck = _deckLoader.LoadDeckFromSingleCSV(tempPath);
            var success = await _sessionService.JoinGame(gameId, playerName, playerDeck.CivicDeck, playerDeck.MilitaryDeck);

            return success ? Ok() : BadRequest("Could not join game");
        }
    }
}
