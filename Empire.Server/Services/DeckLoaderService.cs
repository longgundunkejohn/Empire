using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using Empire.Server.Interfaces;
using Empire.Server.Parsing;
using Empire.Shared.Models;
using Empire.Shared.Models.DTOs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Empire.Server.Services
{
    public class DeckLoaderService
    {
        private readonly ILogger<DeckLoaderService> _logger;
        private readonly ICardDatabaseService _cardDatabase;
        private readonly IMongoCollection<RawDeckEntry> _deckCollection;
        private readonly string _imagePath;

        public Dictionary<int, string> CardDictionary { get; private set; } = new();

        public DeckLoaderService(
            IHostEnvironment env,
            IMongoDbService mongo,
            ILogger<DeckLoaderService> logger,
            ICardDatabaseService cardDatabase)
        {
            _logger = logger;
            _cardDatabase = cardDatabase;
            _imagePath = Path.Combine(env.ContentRootPath, "wwwroot", "images", "Cards");

            _deckCollection = mongo.DeckDatabase.GetCollection<RawDeckEntry>("PlayerDecks");

            LoadImageMappings();
            _logger.LogInformation("DeckLoaderService initialized with {Count} image mappings.", CardDictionary.Count);
        }

        // 👇 ✅ Called when user uploads a deck CSV (returns parsed deck only)
        public List<RawDeckEntry> ParseDeckFromCsv(Stream csvStream)
        {
            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Context.RegisterClassMap<RawDeckEntryMap>();
            var entries = csv.GetRecords<RawDeckEntry>().ToList();

            _logger.LogInformation("Parsed {Count} deck entries from CSV.", entries.Count);
            return entries;
        }

        // 👇 ✅ Converts parsed CSV deck to grouped `PlayerDeck` format
        public PlayerDeck ConvertRawDeckToPlayerDeck(List<RawDeckEntry> rawDeck)
        {
            var civic = new List<int>();
            var military = new List<int>();

            foreach (var entry in rawDeck)
            {
                for (int i = 0; i < entry.Count; i++)
                {
                    if (entry.DeckType == "Civic")
                        civic.Add(entry.CardId);
                    else if (entry.DeckType == "Military")
                        military.Add(entry.CardId);
                }
            }

            return new PlayerDeck(civic, military);
        }

        // 👇 ✅ Optional: Store parsed deck in MongoDB for a player
        public void SaveDeckToDatabase(string player, List<RawDeckEntry> rawDeck)
        {
            // Remove existing
            var filter = Builders<RawDeckEntry>.Filter.Eq("Player", player);
            _deckCollection.DeleteMany(filter);

            // Re-insert with player tag
            foreach (var entry in rawDeck)
                entry.DeckType = entry.DeckType.Trim(); // safety

            _deckCollection.InsertMany(rawDeck.Select(entry =>
            {
                entry.GetType().GetProperty("Player")?.SetValue(entry, player); // manually add Player field
                return entry;
            }));

            _logger.LogInformation("Saved deck for player {Player} with {Count} entries.", player, rawDeck.Count);
        }

        // 👇 ✅ Load deck from MongoDB (used by GameState logic)
        public PlayerDeck LoadDeck(string player)
        {
            var filter = Builders<RawDeckEntry>.Filter.Eq("Player", player);
            var rawDeck = _deckCollection.Find(filter).ToList();

            if (!rawDeck.Any())
            {
                _logger.LogWarning("No deck found in DB for player {Player}.", player);
                return new PlayerDeck();
            }

            return ConvertRawDeckToPlayerDeck(rawDeck);
        }

        // 👇 ✅ Image helper
        private void LoadImageMappings()
        {
            if (!Directory.Exists(_imagePath)) return;

            foreach (var file in Directory.GetFiles(_imagePath, "*.jpg"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var parts = name.Split(' ', 2);
                if (int.TryParse(parts[0], out int cardId))
                {
                    CardDictionary[cardId] = $"images/Cards/{Path.GetFileName(file)}";
                }
            }
        }

        public string GetImagePath(int cardId)
        {
            return CardDictionary.TryGetValue(cardId, out var path)
                ? path
                : "images/Cards/placeholder.jpg";
        }

        public string GetCardDisplayName(int cardId)
        {
            return CardDictionary.TryGetValue(cardId, out var fileName)
                ? Path.GetFileNameWithoutExtension(fileName).Split(" ", 2).Last()
                : "Unknown Card";
        }
    }
}
