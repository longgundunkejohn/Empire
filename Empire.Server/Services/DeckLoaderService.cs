using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Empire.Shared.Models;
using MongoDB.Driver;
using Empire.Server.Interfaces;
using System.Formats.Asn1;
using System.Globalization;
using CsvHelper;

namespace Empire.Server.Services
{
    public class DeckLoaderService
    {
        private readonly ILogger<DeckLoaderService> _logger;
        private readonly string _imagePath;
        private readonly string _csvFile;
        private readonly IMongoCollection<CardData> _cardCollection;

        public Dictionary<int, string> CardDictionary { get; private set; } = new();

        public DeckLoaderService(IHostEnvironment env, IMongoDbService mongo, ILogger<DeckLoaderService> logger)
        {
            _logger = logger;

            _imagePath = Path.Combine(env.ContentRootPath, "wwwroot", "images");
            _csvFile = Path.Combine(env.ContentRootPath, "wwwroot", "cards.csv");

            _cardCollection = mongo.GetDatabase().GetCollection<CardData>("Cards");

            LoadCardMappings();

            _logger.LogInformation("DeckLoaderService loaded {Count} mappings", CardDictionary.Count);
        }

        private void LoadCardMappings()
        {
            if (File.Exists(_csvFile))
            {
                LoadFromCSV();
            }
            else
            {
                GenerateCardMappings();
                SaveToCSV();
            }
        }

        private void LoadFromCSV()
        {
            foreach (var line in File.ReadAllLines(_csvFile))
            {
                var parts = line.Split(',', 2);
                if (int.TryParse(parts[0], out int cardId))
                {
                    CardDictionary[cardId] = parts[1];
                }
            }
        }

        private void GenerateCardMappings()
        {
            CardDictionary.Clear();

            if (!Directory.Exists(_imagePath))
                return;

            foreach (var file in Directory.GetFiles(_imagePath, "*.jpg"))
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                string[] parts = fileName.Split(' ', 2);
                if (parts.Length > 0 && int.TryParse(parts[0], out int cardId))
                {
                    CardDictionary[cardId] = $"images/{fileName}.jpg";
                }
            }
        }

        private void SaveToCSV()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_csvFile)!);
            File.WriteAllLines(_csvFile, CardDictionary.Select(kv => $"{kv.Key},{kv.Value}"));
        }

        public void UpdateMappings()
        {
            GenerateCardMappings();
            SaveToCSV();
        }

        public string GetImagePath(int cardId)
        {
            return CardDictionary.TryGetValue(cardId, out var path) ? path : "images/Cards/placeholder.jpg";
        }

        public string GetCardDisplayName(int cardId)
        {
            return CardDictionary.TryGetValue(cardId, out var fileName)
                ? Path.GetFileNameWithoutExtension(fileName).Split(" ", 2).Last()
                : "Unknown Card";
        }

        public PlayerDeck LoadDeckFromSingleCSV(string filePath)
        {
            var civicDeck = new List<int>();
            var militaryDeck = new List<int>();

            try
            {
                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
                    try
                    {
                        // 🧠 Attempt to get Card ID from either possible header
                        string? cardIdStr = null;
                        if (!(csv.TryGetField("Card ID", out cardIdStr) || csv.TryGetField("CardId", out cardIdStr)))
                        {
                            _logger.LogWarning("❌ Missing Card ID in row, skipping.");
                            continue;
                        }

                        if (!int.TryParse(cardIdStr, out int cardId))
                        {
                            _logger.LogWarning("❌ Could not parse Card ID '{CardIdStr}'", cardIdStr);
                            continue;
                        }

                        int count = csv.GetField<int>("Count");

                        // 🔍 Lookup the card in MongoDB
                        var cardData = _cardCollection.Find(cd => cd.CardID == cardId).FirstOrDefault();
                        if (cardData == null)
                        {
                            _logger.LogWarning("❌ Card ID {CardId} not found in MongoDB", cardId);
                            continue;
                        }

                        // 🏷️ Decide whether it belongs in Civic or Military
                        bool isCivic = cardData.CardType?.Equals("Villager", StringComparison.OrdinalIgnoreCase) == true ||
                                       cardData.CardType?.Equals("Settlement", StringComparison.OrdinalIgnoreCase) == true;

                        var targetDeck = isCivic ? civicDeck : militaryDeck;
                        for (int i = 0; i < count; i++)
                        {
                            targetDeck.Add(cardId);
                        }

                        _logger.LogInformation("✅ Added {Count}x {CardName} (ID {CardId}) to {DeckType}",
                            count, cardData.Name, cardId, isCivic ? "Civic" : "Military");
                    }
                    catch (Exception exRow)
                    {
                        _logger.LogError(exRow, "⚠️ Failed to parse row in CSV.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Error loading deck from CSV at {FilePath}", filePath);
                throw;
            }

            return new PlayerDeck
            {
                CivicDeck = civicDeck,
                MilitaryDeck = militaryDeck
            };
        }


    }
}
