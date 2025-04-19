// === FILE: PreLobbyController.cs ===
using Empire.Server.Services;
using Empire.Shared.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using CsvHelper;
using System.Globalization;
using Empire.Server.Interfaces;
using Empire.Shared.Models;
using MongoDB.Driver;
using MongoDB.Bson;

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

            // ✅ Tag each entry with Player and trimmed type
            foreach (var entry in rawDeck)
            {
                entry.Player = playerName;
                entry.DeckType = entry.DeckType?.Trim();
            }

            // ✅ Store raw deck entries
            var rawCollection = _deckLoader.GetRawDeckCollection();
            var filter = Builders<RawDeckEntry>.Filter.Eq("Player", playerName);
            await rawCollection.DeleteManyAsync(filter);
            await rawCollection.InsertManyAsync(rawDeck);

            // ✅ Assign DeckName before hydration
            string finalDeckName = string.IsNullOrWhiteSpace(deckName)
                ? $"Deck_{Guid.NewGuid():N}".Substring(0, 6)
                : deckName;

            // ✅ Hydrate + preserve existing ID if needed
            var civicDeck = rawDeck
                .Where(e => e.DeckType?.ToLowerInvariant() == "civic")
                .SelectMany(e => Enumerable.Repeat(e.CardId, e.Count))
                .ToList();

            var militaryDeck = rawDeck
                .Where(e => e.DeckType?.ToLowerInvariant() == "military")
                .SelectMany(e => Enumerable.Repeat(e.CardId, e.Count))
                .ToList();

            var existing = await _deckCollection.Find(d =>
                d.PlayerName == playerName && d.DeckName == finalDeckName
            ).FirstOrDefaultAsync();

            var playerDeck = new PlayerDeck(playerName, civicDeck, militaryDeck, finalDeckName)
            {
                Id = existing?.Id ?? ObjectId.GenerateNewId().ToString()
            };

            await _deckCollection.ReplaceOneAsync(
                Builders<PlayerDeck>.Filter.Eq(d => d.Id, playerDeck.Id),
                playerDeck,
                new ReplaceOptions { IsUpsert = true }
            );

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
