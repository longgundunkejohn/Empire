using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Empire.Shared.Models;
using MongoDB.Driver;
using Empire.Server.Interfaces;

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

        public PlayerDeck LoadDeckFromSingleCSV(string csvPath)
        {
            var civic = new List<int>();
            var military = new List<int>();

            if (!File.Exists(csvPath)) throw new FileNotFoundException("Deck file not found", csvPath);

            var lines = File.ReadAllLines(csvPath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');

                if (parts.Length < 3) continue; // ID, Name, Count
                if (!int.TryParse(parts[0], out int cardId)) continue;
                if (!int.TryParse(parts[2], out int count)) continue;

                var data = _cardCollection.Find(c => c.CardID == cardId).FirstOrDefault();
                if (data == null) continue;

                var targetList = (data.CardType.Equals("villager", StringComparison.OrdinalIgnoreCase) ||
                                  data.CardType.Equals("settlement", StringComparison.OrdinalIgnoreCase))
                                  ? civic : military;

                for (int i = 0; i < count; i++)
                {
                    targetList.Add(cardId);
                }
            }

            return new PlayerDeck(civic, military);
        }
    }
}
