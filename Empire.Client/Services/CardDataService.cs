using Empire.Shared.Models;
using System.Text.Json;

namespace Empire.Client.Services
{
    public class CardDataService
    {
        private readonly HttpClient _httpClient;
        private List<CardData>? _allCards;
        private Dictionary<int, CardData>? _cardLookup;

        public CardDataService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<CardData>> GetAllCardsAsync()
        {
            if (_allCards == null)
            {
                await LoadCardsAsync();
            }
            return _allCards ?? new List<CardData>();
        }

        public async Task<CardData?> GetCardByIdAsync(int cardId)
        {
            if (_cardLookup == null)
            {
                await LoadCardsAsync();
            }
            return _cardLookup?.GetValueOrDefault(cardId);
        }

        public async Task<List<CardData>> GetCardsByTypeAsync(string cardType)
        {
            var allCards = await GetAllCardsAsync();
            return allCards.Where(c => c.CardType.Contains(cardType, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task<List<CardData>> GetCardsByTierAsync(string tier)
        {
            var allCards = await GetAllCardsAsync();
            return allCards.Where(c => c.Tier == tier).ToList();
        }

        public async Task<List<CardData>> GetCardsByFactionAsync(string faction)
        {
            var allCards = await GetAllCardsAsync();
            return allCards.Where(c => c.Faction.Equals(faction, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task<List<CardData>> SearchCardsAsync(string searchTerm)
        {
            var allCards = await GetAllCardsAsync();
            return allCards.Where(c => 
                c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                c.CardText.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                c.CardType.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        public async Task<List<CardData>> GetUnitsAsync()
        {
            return await GetCardsByTypeAsync("Unit");
        }

        public async Task<List<CardData>> GetTacticsAsync()
        {
            return await GetCardsByTypeAsync("Tactic");
        }

        public async Task<List<CardData>> GetChroniclesAsync()
        {
            return await GetCardsByTypeAsync("Chronicle");
        }

        public async Task<List<CardData>> GetSettlementsAsync()
        {
            return await GetCardsByTypeAsync("Settlement");
        }

        public async Task<List<CardData>> GetVillagersAsync()
        {
            return await GetCardsByTypeAsync("Villager");
        }

        public string GetCardImageUrl(int cardId)
        {
            // Images are stored as CardID.jpg in wwwroot/images/Cards/
            return $"/images/Cards/{cardId}.jpg";
        }

        public string GetCardImageUrl(CardData card)
        {
            // Use the CardID to get the image
            return GetCardImageUrl(card.CardID);
        }

        private async Task LoadCardsAsync()
        {
            try
            {
                // Try to load from local JSON file first (for development)
                var jsonString = await _httpClient.GetStringAsync("/empire_cards.json");
                var cards = JsonSerializer.Deserialize<List<CardDataDto>>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (cards != null)
                {
                    _allCards = cards.Select(ConvertFromDto).ToList();
                    _cardLookup = _allCards.ToDictionary(c => c.CardID, c => c);
                }
            }
            catch
            {
                // Fallback to API if local file doesn't exist
                try
                {
                    var response = await _httpClient.GetAsync("/api/deckbuilder/cards");
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        var cards = JsonSerializer.Deserialize<List<CardData>>(jsonString, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        
                        if (cards != null)
                        {
                            _allCards = cards;
                            _cardLookup = _allCards.ToDictionary(c => c.CardID, c => c);
                        }
                    }
                }
                catch
                {
                    // If all else fails, return empty list
                    _allCards = new List<CardData>();
                    _cardLookup = new Dictionary<int, CardData>();
                }
            }
        }

        private CardData ConvertFromDto(CardDataDto dto)
        {
            return new CardData
            {
                CardID = dto.CardID,
                Name = dto.Name ?? "",
                CardText = dto.CardText ?? "",
                CardType = dto.CardType ?? "",
                Tier = dto.Tier ?? "",
                Cost = dto.Cost,
                Attack = dto.Attack,
                Defence = dto.Defence,
                Unique = dto.Unique ?? "No",
                Faction = dto.Faction ?? "No",
                ImageFileName = $"{dto.CardID}.jpg" // Set the image filename
            };
        }
    }

    // DTO class to match the MongoDB structure
    public class CardDataDto
    {
        public string? _id { get; set; }
        public int CardID { get; set; }
        public string? Name { get; set; }
        public string? CardText { get; set; }
        public string? CardType { get; set; }
        public string? Tier { get; set; }
        public int Cost { get; set; }
        public int Attack { get; set; }
        public int Defence { get; set; }
        public string? Unique { get; set; }
        public string? Faction { get; set; }
        public string? ImageFileName { get; set; }
    }
}
