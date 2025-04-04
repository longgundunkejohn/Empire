using CsvHelper;
using Empire.Shared.Models.DTOs;
using Empire.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly ILogger<GameController> _logger;

    public GameController(ILogger<GameController> logger)
    {
        _logger = logger;
    }

    [HttpPost("create")]
    public IActionResult CreateGame([FromBody] GameStartRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Player1))
            return BadRequest("Player1 is required.");

        // Create game logic
        var gameId = Guid.NewGuid().ToString();

        // Store to DB / Memory / File / wherever
        _logger.LogInformation("Game created with ID: {GameId} by player {Player1}", gameId, request.Player1);

        return Ok(gameId); // <-- returns gameId as string
    }

    [HttpPost("uploadDeck/{gameId}")]
    public async Task<IActionResult> UploadDeck(string gameId, [FromForm] IFormFile deckCsv, [FromForm] string playerName)
    {
        if (deckCsv == null || string.IsNullOrWhiteSpace(playerName))
            return BadRequest("Missing file or playerName.");

        using var reader = new StreamReader(deckCsv.OpenReadStream());
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var records = csv.GetRecords<DeckCsvEntry>().ToList(); // ← likely fails here

        // Example structure:
        var civic = records
            .Where(r => r.DeckType.Equals("Civic", StringComparison.OrdinalIgnoreCase))
            .Select(r => r.CardId)
            .ToList();

        var military = records
            .Where(r => r.DeckType.Equals("Military", StringComparison.OrdinalIgnoreCase))
            .Select(r => r.CardId)
            .ToList();

        var playerDeck = new PlayerDeck(civic, military);

        // TODO: Store this in your game state store (DB/memory/whatever)

        return Ok();
    }

}
