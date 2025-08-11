// === FILE: PreLobbyController.cs ===
using Empire.Server.Services;
using Empire.Shared.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using CsvHelper;
using System.Globalization;
using Empire.Shared.Models;

namespace Empire.Server.Controllers
{
    [ApiController]
    [Route("api/prelobby")]
    public class PreLobbyController : ControllerBase
    {
        private readonly DeckLoaderService _deckLoader;
        private readonly UserService _userService;
        private readonly ILogger<PreLobbyController> _logger;

        public PreLobbyController(
            DeckLoaderService deckLoader, 
            UserService userService,
            ILogger<PreLobbyController> logger)
        {
            _deckLoader = deckLoader;
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("decks")]
        public async Task<ActionResult<List<string>>> GetAllDeckNames()
        {
            try
            {
                var playerNames = _deckLoader.GetAllPlayerNames();
                return Ok(playerNames);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting all deck names");
                return StatusCode(500, "Error retrieving deck names");
            }
        }

        [HttpGet("decks/{playerName}")]
        public async Task<ActionResult<List<PlayerDeck>>> GetDecksForPlayer(string playerName)
        {
            try
            {
                var deck = _deckLoader.LoadDeck(playerName);
                var decks = new List<PlayerDeck>();
                
                if (deck.CivicDeck.Count > 0 || deck.MilitaryDeck.Count > 0)
                {
                    decks.Add(deck);
                }
                
                return Ok(decks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting decks for player {Player}", playerName);
                return StatusCode(500, "Error retrieving player decks");
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDeck([FromQuery] string playerName, [FromQuery] string? deckName, IFormFile file)
        {
            try
            {
                if (!Request.HasFormContentType || file == null || file.Length == 0)
                    return BadRequest("Expected multipart/form-data content with a valid CSV file.");

                if (string.IsNullOrWhiteSpace(playerName))
                    return BadRequest("Player name is required.");

                List<RawDeckEntry> rawDeck;
                try
                {
                    using var stream = file.OpenReadStream();
                    rawDeck = _deckLoader.ParseDeckFromCsv(stream);
                }
                catch (Exception ex)
                {
                    return BadRequest($"CSV parsing failed: {ex.Message}");
                }

                if (!rawDeck.Any())
                    return BadRequest("Parsed deck is empty.");

                foreach (var entry in rawDeck)
                {
                    entry.Player = playerName;
                    entry.DeckType = entry.DeckType?.Trim();
                }

                // Save the raw deck entries
                _deckLoader.SaveDeckToDatabase(playerName, rawDeck);

                // Create a deck ID for response
                var deckId = Guid.NewGuid().ToString();

                _logger.LogInformation("✅ Deck uploaded for player {Player} with {Count} entries", playerName, rawDeck.Count);
                return Ok(new { message = "✅ Deck uploaded and saved.", deckId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error uploading deck for player {Player}", playerName);
                return StatusCode(500, "Error uploading deck");
            }
        }

        [HttpGet("hasdeck/{playerName}")]
        public async Task<IActionResult> HasDeck(string playerName)
        {
            try
            {
                var deck = _deckLoader.LoadDeck(playerName);
                bool hasDeck = deck.CivicDeck.Count > 0 || deck.MilitaryDeck.Count > 0;
                
                return Ok(new { player = playerName, hasDeck });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking deck for player {Player}", playerName);
                return StatusCode(500, "Error checking deck");
            }
        }

        [HttpGet("deck/{playerName}/{deckId}")]
        public async Task<IActionResult> GetSpecificDeck(string playerName, string deckId)
        {
            try
            {
                var deck = _deckLoader.LoadDeck(playerName);
                
                if (deck.CivicDeck.Count == 0 && deck.MilitaryDeck.Count == 0)
                    return NotFound($"No deck found for {playerName}");

                return Ok(deck);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting specific deck for player {Player}", playerName);
                return StatusCode(500, "Error retrieving deck");
            }
        }

        [HttpDelete("deck/{playerName}")]
        public async Task<IActionResult> DeleteDeck(string playerName)
        {
            try
            {
                // For now, we can't easily delete from in-memory storage
                // This would need to be implemented in DeckLoaderService
                _logger.LogWarning("⚠️ Delete deck not implemented for in-memory storage");
                return Ok(new { message = $"Delete operation noted for {playerName} (not implemented for in-memory storage)" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting deck for player {Player}", playerName);
                return StatusCode(500, "Error deleting deck");
            }
        }
    }
}
