﻿using System.Globalization;
using CsvHelper;
using Empire.Server.Interfaces;
using Empire.Shared.Models.DTOs;
using MongoDB.Driver;
using Empire.Shared.Models;

namespace Empire.Server.Services
{
    public class DeckLoaderService
    {
        private readonly IMongoCollection<RawDeckEntry> _rawDeckCollection;
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
            _rawDeckCollection = mongo.DeckDatabase.GetCollection<RawDeckEntry>("RawDeckEntries");

            LoadImageMappings();
            _logger.LogInformation("✅ DeckLoaderService initialized with {Count} image mappings.", CardDictionary.Count);
        }

        public List<RawDeckEntry> ParseDeckFromCsv(Stream csvStream)
        {
            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Context.RegisterClassMap<RawDeckEntryMap>();
            var entries = csv.GetRecords<RawDeckEntry>().ToList();

            _logger.LogInformation("📄 Parsed {Count} deck entries from CSV.", entries.Count);
            return entries;
        }

        public PlayerDeck ConvertRawDeckToPlayerDeck(string playerName, List<RawDeckEntry> rawDeck)
        {
            var civicDeck = new List<int>();
            var militaryDeck = new List<int>();

            foreach (var entry in rawDeck)
            {
                var type = entry.DeckType?.Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(type))
                {
                    type = DeckUtils.IsCivicCard(entry.CardId) ? "civic" : "military";
                }

                for (int i = 0; i < entry.Count; i++)
                {
                    if (type == "civic") civicDeck.Add(entry.CardId);
                    else if (type == "military") militaryDeck.Add(entry.CardId);
                    else _logger.LogWarning("❓ Unknown deck type '{DeckType}' for card ID {CardId}", entry.DeckType, entry.CardId);
                }
            }

            _logger.LogInformation("🛠 Built deck for {Player} — Civic: {CivicCount}, Military: {MilitaryCount}",
                playerName, civicDeck.Count, militaryDeck.Count);

            return new PlayerDeck(playerName, civicDeck, militaryDeck);
        }




        public void SaveDeckToDatabase(string player, List<RawDeckEntry> rawDeck)
        {
            var filter = Builders<RawDeckEntry>.Filter.Eq("Player", player);
            _rawDeckCollection.DeleteMany(filter);

            foreach (var entry in rawDeck)
            {
                entry.DeckType = entry.DeckType?.Trim();
                entry.Player = player;
            }

            _rawDeckCollection.InsertMany(rawDeck);
            _logger.LogInformation("💾 Saved raw deck for player {Player} with {Count} entries.", player, rawDeck.Count);
        }


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

        private void LoadImageMappings()
        {
            if (!Directory.Exists(_imagePath))
            {
                _logger.LogWarning("📁 Card image folder not found: {Path}", _imagePath);
                return;
            }

            foreach (var file in Directory.GetFiles(_imagePath, "*.jpg"))
            {
                var name = Path.GetFileNameWithoutExtension(file);

                if (int.TryParse(name, out int cardId))
                {
                    var imagePath = $"images/Cards/{Path.GetFileName(file)}";
                    CardDictionary[cardId] = imagePath;
                }
                else
                {
                    _logger.LogWarning("⚠️ Could not parse CardId from filename: {File}", name);
                }
            }

            if (CardDictionary.Count == 0)
            {
                _logger.LogError("❌ No valid image mappings loaded.");
            }
            else
            {
                _logger.LogInformation("✅ Loaded {Count} card images from disk.", CardDictionary.Count);
            }
        }


        public string GetImagePath(int cardId)
        {
            if (CardDictionary.Count == 0)
            {
                _logger.LogWarning("⚠️ CardDictionary is empty, attempting to reload image mappings.");
                LoadImageMappings();
            }

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

        public IMongoCollection<RawDeckEntry> GetRawDeckCollection()
        {
            return _rawDeckCollection;
        }


    }
}
