using Empire.Server.Services;
using Empire.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Empire.Shared.DTOs;

namespace Empire.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly GameSessionService _sessionService;
        private readonly CardFactory _cardFactory;
        private readonly DeckLoaderService _deckLoader;

        public GameController(GameSessionService sessionService, CardFactory cardFactory, DeckLoaderService deckLoader)
        {
            _sessionService = sessionService;
            _cardFactory = cardFactory;
            _deckLoader = deckLoader; //
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
        [HttpPost("create")]
        public async Task<ActionResult<string>> CreateGame([FromForm] IFormFile deckCsv, [FromForm] string playerId)
        {
            if (deckCsv == null || deckCsv.Length == 0)
                return BadRequest("CSV is required.");

            var tempPath = Path.GetTempFileName();
            using (var stream = System.IO.File.Create(tempPath))
            {
                await deckCsv.CopyToAsync(stream);
            }

            var playerDeck = _deckLoader.LoadDeckFromSingleCSV(tempPath);

            var gameId = await _sessionService.CreateGameSession(playerId, ""); // player1 = host, no player2 yet
            var gameState = await _sessionService.GetGameState(gameId);
            gameState.PlayerDecks[playerId] = playerDeck;
            gameState.PlayerHands[playerId] = new List<int>(); // empty hand
            gameState.PlayerBoard[playerId] = new List<int>();
            gameState.PlayerGraveyards[playerId] = new List<int>();

            // 🛑 DO NOT apply a move here that might trigger game full logic
            // Leave as-is so player2 can still join and it shows up in /open

            return Ok(gameId);
        }

        [HttpGet("state/{gameId}/{playerId}")]
        public async Task<ActionResult<GameState>> GetGameState(string gameId, string playerId)
        {
            var state = await _sessionService.GetGameState(gameId);
            if (state == null) return NotFound();

            return Ok(state);
        }
        [HttpPost("move")]
        public async Task<IActionResult> SubmitMove([FromBody] GameMove move, [FromQuery] string gameId)
        {
            var result = await _sessionService.ApplyMove(gameId, move);
            return result ? Ok() : BadRequest("Invalid move");
        }
        [HttpPost("join/{gameId}/{playerId}")]
        public async Task<IActionResult> JoinGame(
            [FromRoute] string gameId,
            [FromRoute] string playerId,
            [FromBody] PlayerDeck playerDeck)
        {
            // Use the existing _sessionService, not _gameSessionService
            var gameState = await _sessionService.GetGameState(gameId);

            if (gameState == null) return NotFound("Game not found.");

            // Assign the player's decks
            gameState.PlayerDecks[playerId] = playerDeck;

            // Add any other logic for updating the game state

            await _sessionService.ApplyMove(gameId, new GameMove
            {
                PlayerId = playerId,
                MoveType = "JoinGame"
            });

            return Ok(gameId);
        }




    }
}
