using Empire.Server.Interfaces;
using Empire.Shared.Models.DTOs;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;

namespace Empire.Server.Services
{
    public class DeckService
    {
        private readonly IMongoCollection<RawDeckEntry> _deckCollection;
        private readonly ILogger<DeckService> _logger;

        public DeckService(IMongoDbService mongo, ILogger<DeckService> logger)
        {
            _deckCollection = mongo.DeckDatabase.GetCollection<RawDeckEntry>("PlayerDecks");
            _logger = logger;
        }

        public async Task SaveDeckAsync(string playerName, List<RawDeckEntry> deck)
        {
            // Remove old deck if it exists
            await _deckCollection.DeleteManyAsync(d => d.Player == playerName);

            foreach (var entry in deck)
            {
                entry.Player = playerName;
            }

            await _deckCollection.InsertManyAsync(deck);
            _logger.LogInformation("✅ Saved deck for player {Player}.", playerName);
        }

        public async Task<List<RawDeckEntry>> GetDeckAsync(string playerName)
        {
            return await _deckCollection.Find(d => d.Player == playerName).ToListAsync();
        }

        public async Task<bool> HasDeckAsync(string playerName)
        {
            var count = await _deckCollection.CountDocumentsAsync(d => d.Player == playerName);
            return count > 0;
        }

        public async Task DeleteDeckAsync(string playerName)
        {
            await _deckCollection.DeleteManyAsync(d => d.Player == playerName);
            _logger.LogInformation("🗑 Deleted deck for player {Player}.", playerName);
        }
    }
}
