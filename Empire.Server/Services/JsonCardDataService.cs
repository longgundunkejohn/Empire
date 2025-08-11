using Empire.Shared.Models;
using Empire.Server.Interfaces;
using System.Text.Json;

namespace Empire.Server.Services
{
    public class JsonCardDataService : ICardDatabaseService
    {
        private readonly List<CardData> _cards;
        private readonly ILogger<JsonCardDataService> _logger;

        public JsonCardDataService(IWebHostEnvironment environment, ILogger<JsonCardDataService> logger)
        {
            _logger = logger;
            _cards = LoadCardsFromJson(environment);
        }

        private List<CardData> LoadCardsFromJson(IWebHostEnvironment environment)
        {
            try
            {
                var jsonPath = Path.Combine(environment.ContentRootPath, "empire_cards.json");
                if (!File.Exists(jsonPath))
                {
                    _logger.LogWarning($"Card data file not found at {jsonPath}");
                    return new List<CardData>();
                }

                var jsonContent = File.ReadAllText(jsonPath);
                var cards = JsonSerializer.Deserialize<List<CardData>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation($"Loaded {cards?.Count ?? 0} cards from JSON");
                return cards ?? new List<CardData>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load cards from JSON");
                return new List<CardData>();
            }
        }

        public IEnumerable<CardData> GetAllCards() => _cards;

        public CardData? GetCardById(int id)
        {
            return _cards.FirstOrDefault(c => c.CardID == id);
        }

        public async Task<List<Card>> GetDeckCards(List<int> cardIds)
        {
            var allCardData = _cards
                .Where(cd => cardIds.Contains(cd.CardID))
                .ToDictionary(cd => cd.CardID, cd => cd);

            var result = new List<Card>();

            foreach (var id in cardIds)
            {
                if (allCardData.TryGetValue(id, out var cd))
                {
                    result.Add(new Card
                    {
                        CardId = cd.CardID,
                        Name = cd.Name,
                        CardText = cd.CardText,
                        Faction = cd.Faction,
                        Type = cd.CardType,
                        ImagePath = $"images/Cards/{cd.CardID}.jpg", // Fixed image path
                        IsExerted = false,
                        CurrentDamage = 0,
                        Cost = cd.Cost,
                        Attack = cd.Attack,
                        Defence = cd.Defence,
                        Tier = cd.Tier,
                        Unique = cd.Unique
                    });
                }
                else
                {
                    _logger.LogWarning($"Card ID {id} not found in database");
                }
            }

            _logger.LogInformation($"Hydrated {result.Count} cards from list of {cardIds.Count} IDs");
            return result;
        }

        public List<CardData> GetCardsByType(string cardType)
        {
            return _cards.Where(c => c.CardType.Contains(cardType, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<CardData> GetCardsByFaction(string faction)
        {
            return _cards.Where(c => c.Faction.Equals(faction, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<CardData> GetCardsByTier(string tier)
        {
            return _cards.Where(c => c.Tier.Equals(tier, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<CardData> SearchCards(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return _cards.ToList();

            return _cards.Where(c => 
                c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                c.CardText.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }
    }
}
