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
public async Task<ActionResult<List<string>>> GetDeckOwners()
{
    var decks = await _deckCollection.Find(_ => true).ToListAsync();
    return Ok(decks.Select(d => d.PlayerName).Distinct().ToList());
}

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDeck([FromQuery] string playerName, IFormFile file)
        {
            if (string.IsNullOrWhiteSpace(playerName) || file == null || file.Length == 0)
                return BadRequest("Player name and CSV file are required.");

            using var stream = file.OpenReadStream();
            var rawDeck = _deckLoader.ParseDeckFromCsv(stream);

            await _deckService.SaveDeckAsync(playerName, rawDeck);

            return Ok(new { message = "Deck uploaded and saved.", count = rawDeck.Count });
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
