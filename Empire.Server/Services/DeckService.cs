namespace Empire.Server.Services
{
    using Empire.Shared.Models;
    using Empire.Shared.Models.DTOs;
    using MongoDB.Driver;
    using Microsoft.Extensions.Logging;
    using System.Linq;
    using System.Threading.Tasks;
    using Empire.Server.Interfaces;
    using MongoDB.Bson;

    public class DeckService
    {
        private readonly IMongoCollection<PlayerDeck> _deckCollection;
        private readonly IMongoCollection<RawDeckEntry> _rawDeckCollection;
        private readonly ILogger<DeckService> _logger;

        public DeckService(IMongoDbService mongo, ILogger<DeckService> logger)
        {
            _deckCollection = mongo.DeckDatabase.GetCollection<PlayerDeck>("PlayerDecks");
            _rawDeckCollection = mongo.DeckDatabase.GetCollection<RawDeckEntry>("RawDeckEntries"); // Collection for raw entries
            _logger = logger;
        }

        // Save final deck for the player
        public async Task SaveDeckAsync(PlayerDeck deck)
        {
            // Just upsert based on PlayerName, no need to touch .Id
            var filter = Builders<PlayerDeck>.Filter.Eq(d => d.PlayerName, deck.PlayerName);
            await _deckCollection.ReplaceOneAsync(filter, deck, new ReplaceOptions { IsUpsert = true });

            _logger.LogInformation("✅ Saved deck for player {PlayerName}. Civic: {CivicCount}, Military: {MilitaryCount}",
                deck.PlayerName,
                deck.CivicDeck?.Count ?? 0,
                deck.MilitaryDeck?.Count ?? 0);
        }


        // Get the final deck for the player
        public async Task<PlayerDeck> GetDeckAsync(string playerName)
        {
            return await _deckCollection.Find(d => d.PlayerName == playerName).FirstOrDefaultAsync();
        }

        // Check if the player has a deck
        public async Task<bool> HasDeckAsync(string playerName)
        {
            var count = await _deckCollection.CountDocumentsAsync(d => d.PlayerName == playerName);
            return count > 0;
        }

        // Delete the deck of a player
        public async Task DeleteDeckAsync(string playerName)
        {
            await _deckCollection.DeleteManyAsync(d => d.PlayerName == playerName);
            _logger.LogInformation("🗑 Deleted deck for player {Player}.", playerName);
        }

        // Parse RawDeckEntries and save the final PlayerDeck
        public async Task<PlayerDeck> ParseAndSaveDeckAsync(string playerName, string? deckName = null)
        {
            var rawDeckEntries = await _rawDeckCollection.Find(d => d.Player == playerName).ToListAsync();

            if (!rawDeckEntries.Any())
            {
                _logger.LogWarning("No raw deck found for player {Player}.", playerName);
                return null;
            }

            var civicDeck = rawDeckEntries
                .Where(d => d.DeckType?.ToLowerInvariant() == "civic")
                .SelectMany(d => Enumerable.Repeat(d.CardId, d.Count))
                .ToList();

            var militaryDeck = rawDeckEntries
                .Where(d => d.DeckType?.ToLowerInvariant() == "military")
                .SelectMany(d => Enumerable.Repeat(d.CardId, d.Count))
                .ToList();

            // Ensure deckName fallback
            var finalDeckName = string.IsNullOrWhiteSpace(deckName)
                ? $"Deck_{Guid.NewGuid().ToString("N")[..6]}"
                : deckName;

            // Lookup existing deck by player + deck name
            var filter = Builders<PlayerDeck>.Filter.And(
                Builders<PlayerDeck>.Filter.Eq(d => d.PlayerName, playerName),
                Builders<PlayerDeck>.Filter.Eq(d => d.DeckName, finalDeckName)
            );

            var existingDeck = await _deckCollection.Find(filter).FirstOrDefaultAsync();

            var playerDeck = new PlayerDeck(playerName, civicDeck, militaryDeck, finalDeckName)
            {
                Id = existingDeck?.Id ?? ObjectId.GenerateNewId().ToString(),
                DeckName = finalDeckName
            };

            await SaveDeckAsync(playerDeck);

            _logger.LogInformation("✅ Deck for player {PlayerName} saved successfully. DeckName: {DeckName}", playerName, finalDeckName);

            return playerDeck;
        }

        public async Task<List<PlayerDeck>> GetAllDecksAsync()
        {
            return await _deckCollection.Find(_ => true).ToListAsync();
        }

    }
}