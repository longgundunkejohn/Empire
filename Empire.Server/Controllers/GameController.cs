using Empire.Server.Services;
using Empire.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace Empire.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly GameSessionService _sessionService;
        private readonly CardFactory _cardFactory;

        public GameController(GameSessionService sessionService, CardFactory cardFactory)
        {
            _sessionService = sessionService;
            _cardFactory = cardFactory;
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
        var gameId = await _sessionService.CreateGameSession(playerId, ""); // Create solo game for now
        var gameState = await _sessionService.GetGameState(gameId);
        gameState.PlayerDecks[playerId] = playerDeck;
        gameState.PlayerHands[playerId] = new List<int>(); // empty hand
        gameState.PlayerBoard[playerId] = new List<int>();
        gameState.PlayerGraveyards[playerId] = new List<int>();

        await _sessionService.ApplyMove(gameId, new GameMove
        {
            PlayerId = playerId,
            MoveType = "JoinGame"
        });

        return Ok(gameId);
    }

    }
}
