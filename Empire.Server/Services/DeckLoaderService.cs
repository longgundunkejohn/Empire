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

        // ✅ Parse uploaded CSV into raw deck entries
        public List<RawDeckEntry> ParseDeckFromCsv(Stream csvStream)
        {
            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Context.RegisterClassMap<RawDeckEntryMap>();
            var entries = csv.GetRecords<RawDeckEntry>().ToList();

            _logger.LogInformation("Parsed {Count} deck entries from CSV.", entries.Count);
            return entries;
        }

        // ✅ Convert raw CSV entries into grouped PlayerDeck
        public PlayerDeck ConvertRawDeckToPlayerDeck(string playerName, List<RawDeckEntry> rawDeck)
        {
            var civicDeck = new List<int>();
            var militaryDeck = new List<int>();

            foreach (var entry in rawDeck)
            {
                var type = entry.DeckType?.Trim().ToLowerInvariant();
                for (int i = 0; i < entry.Count; i++)
                {
                    if (type == "civic")
                        civicDeck.Add(entry.CardId);
                    else if (type == "military")
                        militaryDeck.Add(entry.CardId);
                    else
                        _logger.LogWarning("Unknown deck type '{DeckType}' for card ID {CardId}", entry.DeckType, entry.CardId);
                }
            }

            _logger.LogInformation("Converted deck for {Player} — Civic: {CivicCount}, Military: {MilitaryCount}", playerName, civicDeck.Count, militaryDeck.Count);
            return new PlayerDeck(playerName, civicDeck, militaryDeck);
        }

        // ✅ Store deck in MongoDB (clears old entries first)
        public void SaveDeckToDatabase(string player, List<RawDeckEntry> rawDeck)
        {
            var filter = Builders<RawDeckEntry>.Filter.Eq("Player", player);
            _deckCollection.DeleteMany(filter);

            foreach (var entry in rawDeck)
            {
                entry.DeckType = entry.DeckType?.Trim();
                entry.GetType().GetProperty("Player")?.SetValue(entry, player);
            }

            _deckCollection.InsertMany(rawDeck);
            _logger.LogInformation("✅ Saved deck for player {Player} with {Count} entries.", player, rawDeck.Count);
        }

        // ✅ Load deck from MongoDB
        public PlayerDeck LoadDeck(string player)
        {
            var filter = Builders<RawDeckEntry>.Filter.Eq("Player", player);
            var rawDeck = _deckCollection.Find(filter).ToList();

            if (!rawDeck.Any())
            {
                _logger.LogWarning("❌ No deck found in DB for player {Player}.", player);
                return new PlayerDeck();
            }

            return ConvertRawDeckToPlayerDeck(player, rawDeck);
        }

        // ✅ Load all card images into a dictionary: CardId => image path
        private void LoadImageMappings()
        {
            if (!Directory.Exists(_imagePath))
            {
                _logger.LogWarning("Card image folder not found: {Path}", _imagePath);
                return;
            }

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

        // ✅ Get image path from card ID
        public string GetImagePath(int cardId)
        {
            return CardDictionary.TryGetValue(cardId, out var path)
                ? path
                : "images/Cards/placeholder.jpg";
        }

        // ✅ Display-friendly name from file
        public string GetCardDisplayName(int cardId)
        {
            return CardDictionary.TryGetValue(cardId, out var fileName)
                ? Path.GetFileNameWithoutExtension(fileName).Split(" ", 2).Last()
                : "Unknown Card";
        }
    }
}