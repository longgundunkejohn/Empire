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
        try
        {
            using var reader = new StreamReader(deckCsv.OpenReadStream());
            var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            // Manual mapping since your headers don't match model
            var rawRows = csv.GetRecords<dynamic>().ToList();

            var civicDeck = new List<int>();
            var militaryDeck = new List<int>();

            foreach (dynamic row in rawRows)
            {
                int cardId = int.Parse(row["Card ID"]);
                int count = int.Parse(row["Count"]);

                // Just for demo: civic if ID is even, military if odd
                var targetDeck = cardId % 2 == 0 ? civicDeck : militaryDeck;

                for (int i = 0; i < count; i++)
                    targetDeck.Add(cardId);
            }

            var playerDeck = new PlayerDeck(civicDeck, militaryDeck);

            // Save this to game state
            Console.WriteLine($"Deck uploaded for {playerName}: Civic({civicDeck.Count}), Military({militaryDeck.Count})");

            return Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UploadDeck] Error: {ex.Message}");
            return StatusCode(500, $"Deck upload failed: {ex.Message}");
        }
    }

}
