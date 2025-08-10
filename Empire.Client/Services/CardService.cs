using Empire.Shared.Models;
using System.Text.Json;

namespace Empire.Client.Services
{
    public class CardService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public CardService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<CardData>> GetAllCardsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/card");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var cards = JsonSerializer.Deserialize<List<CardData>>(json, _jsonOptions);
                
                return cards ?? new List<CardData>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching cards: {ex.Message}");
                return new List<CardData>();
            }
        }

        public async Task<CardData?> GetCardByIdAsync(int cardId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/card/{cardId}");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var card = JsonSerializer.Deserialize<CardData>(json, _jsonOptions);
                
                return card;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching card {cardId}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<CardData>> GetCardsByFactionAsync(string faction)
        {
            var allCards = await GetAllCardsAsync();
            return allCards.Where(c => c.Faction.Equals(faction, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task<List<CardData>> GetCardsByTypeAsync(string cardType)
        {
            var allCards = await GetAllCardsAsync();
            return allCards.Where(c => c.CardType.Equals(cardType, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task<List<CardData>> GetCivicCardsAsync()
        {
            var allCards = await GetAllCardsAsync();
            return allCards.Where(c => c.CardType is "Settlement" or "Villager").ToList();
        }

        public async Task<List<CardData>> GetMilitaryCardsAsync()
        {
            var allCards = await GetAllCardsAsync();
            return allCards.Where(c => c.CardType is "Unit" or "Tactic" or "Battle Tactic" or "Chronicle" or "Skirmisher").ToList();
        }
    }
}
