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
        private readonly string _imagePath;
        private readonly string _csvFile;
        private readonly IMongoCollection<CardData> _cardCollection;

        public Dictionary<int, string> CardDictionary { get; private set; } = new();

        public DeckLoaderService(
            IHostEnvironment env,
            IMongoDbService mongo,
            ILogger<DeckLoaderService> logger,
            ICardDatabaseService cardDatabase)
        {
            _logger = logger;
            _cardDatabase = cardDatabase;

            _imagePath = Path.Combine(env.ContentRootPath, "wwwroot", "images");
            _csvFile = Path.Combine(env.ContentRootPath, "wwwroot", "cards.csv");

            _cardCollection = mongo.GetDatabase().GetCollection<CardData>("Cards");

            LoadCardMappings();

            _logger.LogInformation("DeckLoaderService loaded {Count} mappings", CardDictionary.Count);
        }

        private bool IsCivicCard(int cardId)
        {
            int lastTwoDigits = cardId % 100;
            return lastTwoDigits is >= 80 and <= 99;
        }

        public (List<int> CivicDeck, List<int> MilitaryDeck) ParseDeckFromCsv(Stream csvStream)
        {
            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Context.RegisterClassMap<RawDeckEntryMap>();
            var entries = csv.GetRecords<RawDeckEntry>().ToList();

            var civic = new List<int>();
            var military = new List<int>();

            foreach (var entry in entries)
            {
                var target = IsCivicCard(entry.CardId) ? civic : military;

                for (int i = 0; i < entry.Count; i++)
                    target.Add(entry.CardId);
            }

            return (civic, military);
        }

        public void UpdateMappings()
        {
            GenerateCardMappings();
            SaveToCSV();
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
    }
}
