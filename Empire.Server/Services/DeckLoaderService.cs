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

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                if (csv.TryGetField("Card ID", out string? cardIdStr) || csv.TryGetField("CardId", out cardIdStr))
                {
                    if (!int.TryParse(cardIdStr, out var cardId)) continue;

                    int count = csv.GetField<int>("Count");

                    // 🔍 Lookup CardData in MongoDB to determine its type
                    var cardData = _cardCollection.Find(cd => cd.CardID == cardId).FirstOrDefault();
                    if (cardData == null)
                    {
                        Console.WriteLine($"⚠️ Card ID {cardId} not found in DB.");
                        continue;
                    }

                    bool isCivic = cardData.CardType.Equals("Villager", StringComparison.OrdinalIgnoreCase) ||
                                   cardData.CardType.Equals("Settlement", StringComparison.OrdinalIgnoreCase);

                    var targetDeck = isCivic ? civicDeck : militaryDeck;
                    for (int i = 0; i < count; i++)
                    {
                        targetDeck.Add(cardId);
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Skipped a row due to missing Card ID");
                }
            }

            return new PlayerDeck
            {
                CivicDeck = civicDeck,
                MilitaryDeck = militaryDeck
            };
        }

    }
}
