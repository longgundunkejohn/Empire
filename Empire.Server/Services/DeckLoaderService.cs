using System.Globalization;
using CsvHelper;
using Empire.Shared.Models.DTOs;
using Empire.Shared.Models;
using System.Collections.Concurrent;

namespace Empire.Server.Services
{
    public class DeckLoaderService
    {
        private readonly ILogger<DeckLoaderService> _logger;
        private readonly ICardDatabaseService _cardDatabase;
        private readonly string _imagePath;
        private readonly ConcurrentDictionary<string, List<RawDeckEntry>> _playerDecks;

        public Dictionary<int, string> CardDictionary { get; private set; } = new();

        public DeckLoaderService(
            IHostEnvironment env,
            ILogger<DeckLoaderService> logger,
            ICardDatabaseService cardDatabase)
        {
            _logger = logger;
            _cardDatabase = cardDatabase;
            _imagePath = Path.Combine(env.ContentRootPath, "wwwroot", "images", "Cards");
            _playerDecks = new ConcurrentDictionary<string, List<RawDeckEntry>>();

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
            foreach (var entry in rawDeck)
            {
                entry.DeckType = entry.DeckType?.Trim();
                entry.Player = player;
            }

            _playerDecks.AddOrUpdate(player, rawDeck, (key, oldValue) => rawDeck);
            _logger.LogInformation("💾 Saved raw deck for player {Player} with {Count} entries.", player, rawDeck.Count);
        }

        public PlayerDeck LoadDeck(string player)
        {
            if (!_playerDecks.TryGetValue(player, out var rawDeck) || !rawDeck.Any())
            {
                _logger.LogWarning("❌ No deck found in memory for player {Player}.", player);
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

        public List<RawDeckEntry> GetPlayerDeckEntries(string player)
        {
            return _playerDecks.TryGetValue(player, out var deck) ? deck : new List<RawDeckEntry>();
        }

        public List<string> GetAllPlayerNames()
        {
            return _playerDecks.Keys.ToList();
        }


    }
}
