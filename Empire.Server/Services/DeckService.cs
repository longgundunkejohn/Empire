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
        public async Task<PlayerDeck> ParseAndSaveDeckAsync(string playerName)
        {
            // Retrieve raw deck entries from MongoDB
            var rawDeckEntries = await _rawDeckCollection.Find(d => d.Player == playerName).ToListAsync();

            if (!rawDeckEntries.Any())
            {
                _logger.LogWarning("No raw deck found for player {Player}.", playerName);
                return null; // No deck found for the player
            }

            // Split into Civic and Military Decks
            var civicDeck = rawDeckEntries.Where(d => d.DeckType == "Civic").SelectMany(d => Enumerable.Repeat(d.CardId, d.Count)).ToList();
            var militaryDeck = rawDeckEntries.Where(d => d.DeckType == "Military").SelectMany(d => Enumerable.Repeat(d.CardId, d.Count)).ToList();

            // Create final PlayerDeck object
            var playerDeck = new PlayerDeck(playerName, civicDeck, militaryDeck);

            // Save the PlayerDeck in the PlayerDecks collection
            await SaveDeckAsync(playerDeck);

            _logger.LogInformation("✅ Deck for player {PlayerName} saved successfully.", playerName);

            return playerDeck;
        }
    }
}
