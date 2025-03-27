using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace Empire.Server.Services
{
    public class DeckLoaderService
    {
         readonly ILogger<DeckLoaderService> _logger;
        private readonly string _imagePath;
        private readonly string _csvFile;
        public Dictionary<int, string> CardDictionary { get; private set; } = new();

        public DeckLoaderService(IHostEnvironment env, ILogger<DeckLoaderService> logger)
        {
            _logger = logger;

            try
            {
                _imagePath = Path.Combine(env.ContentRootPath, "wwwroot", "images");
                _csvFile = Path.Combine(env.ContentRootPath, "wwwroot", "cards.csv");

                _logger.LogInformation("DeckLoaderService initializing. Image path: {ImagePath}", _imagePath);

                LoadCardMappings();

                _logger.LogInformation("DeckLoaderService loaded {Count} mappings", CardDictionary.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeckLoaderService crashed!");
                throw;
            }
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

            private void GenerateCardMappings()
            {
                CardDictionary.Clear();

                if (!Directory.Exists(_imagePath))
                    return;

                foreach (var file in Directory.GetFiles(_imagePath, "*.jpg"))
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string[] parts = fileName.Split(' ', 2); // handle names with spaces
                    if (parts.Length > 0 && int.TryParse(parts[0], out int cardId))
                    {
                        // Save image relative path for Blazor
                        CardDictionary[cardId] = $"images/{fileName}.jpg";
                    }
                }
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
                    if (!int.TryParse(parts[0], out int cardId)) continue;

                    var data = _cardCollection.Find(c => c.CardID == cardId).FirstOrDefault(); // lookup card type
                    if (data == null) continue;

                    if (data.CardType.Equals("villager", StringComparison.OrdinalIgnoreCase) ||
                        data.CardType.Equals("settlement", StringComparison.OrdinalIgnoreCase))
                    {
                        civic.Add(cardId);
                    }
                    else
                    {
                        military.Add(cardId);
                    }
                }

                return new PlayerDeck(civic, military);
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
                return CardDictionary.TryGetValue(cardId, out var path) ? path : "images/placeholder.jpg";
            }

            public string GetCardDisplayName(int cardId)
            {
                return CardDictionary.TryGetValue(cardId, out var fileName)
                    ? Path.GetFileNameWithoutExtension(fileName).Split(" ", 2).Last()
                    : "Unknown Card";
            }
        }
    } 

