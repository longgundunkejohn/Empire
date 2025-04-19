// === FILE: PreLobbyController.cs ===
using Empire.Server.Services;
using Empire.Shared.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using CsvHelper;
using System.Globalization;
using Empire.Server.Interfaces;
using Empire.Shared.Models;
using MongoDB.Driver;

namespace Empire.Server.Controllers
{
    [ApiController]
    [Route("api/prelobby")]
    public class PreLobbyController : ControllerBase
    {
        private readonly DeckLoaderService _deckLoader;
        private readonly DeckService _deckService;
        private readonly IMongoCollection<PlayerDeck> _deckCollection;

        public PreLobbyController(DeckLoaderService deckLoader, DeckService deckService, IMongoDbService mongo)
        {
            _deckLoader = deckLoader;
            _deckService = deckService;
            _deckCollection = mongo.DeckDatabase.GetCollection<PlayerDeck>("PlayerDecks");
        }

        [HttpGet("decks")]
        public async Task<ActionResult<List<string>>> GetAllDeckNames()
        {
            var decks = await _deckCollection.Find(_ => true).ToListAsync();

            var names = decks
                .Select(d => d.DeckName ?? "")
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .OrderBy(n => n)
                .ToList();


            return Ok(names);
        }

        [HttpGet("decks/{playerName}")]
        public async Task<ActionResult<List<PlayerDeck>>> GetDecksForPlayer(string playerName)
        {
            var filter = Builders<PlayerDeck>.Filter.Eq(d => d.PlayerName, playerName);
            var decks = await _deckCollection.Find(filter).ToListAsync();
            return Ok(decks);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDeck([FromQuery] string playerName, [FromQuery] string? deckName, IFormFile file)
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

            // Save raw entries to DB (used to hydrate final deck)
            _deckLoader.SaveDeckToDatabase(playerName, rawDeck);

            // Hydrate full deck
            var playerDeck = await _deckService.ParseAndSaveDeckAsync(playerName);

            // Set name BEFORE storing
            playerDeck.DeckName = string.IsNullOrWhiteSpace(deckName)
                ? $"Deck_{Guid.NewGuid().ToString("N")[..6]}"
                : deckName;

            // Upsert using PlayerName + DeckName
            var filter = Builders<PlayerDeck>.Filter.And(
                Builders<PlayerDeck>.Filter.Eq(d => d.PlayerName, playerName),
                Builders<PlayerDeck>.Filter.Eq(d => d.DeckName, playerDeck.DeckName)
            );

            await _deckCollection.ReplaceOneAsync(filter, playerDeck, new ReplaceOptions { IsUpsert = true });

            return Ok(new { message = "✅ Deck uploaded and saved.", deckId = playerDeck.Id });
        }
 
        [HttpGet("hasdeck/{playerName}")]
        public async Task<IActionResult> HasDeck(string playerName)
        {
            bool exists = await _deckService.HasDeckAsync(playerName);
            return Ok(new { player = playerName, hasDeck = exists });
        }

        [HttpGet("deck/{playerName}")]
        public async Task<IActionResult> GetDeck(string playerName)
        {
            var deck = await _deckService.GetDeckAsync(playerName);
            if (deck == null)
                return NotFound($"No deck found for {playerName}");

            return Ok(deck);
        }

        [HttpDelete("deck/{playerName}")]
        public async Task<IActionResult> DeleteDeck(string playerName)
        {
            await _deckService.DeleteDeckAsync(playerName);
            return Ok(new { message = $"Deleted deck for {playerName}" });
        }
    }
}
