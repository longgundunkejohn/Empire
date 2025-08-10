using Empire.Shared.Models;
using System.Text.Json;

namespace Empire.Client.Services
{
    public class DeckService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public DeckService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<Deck>> GetAllDecksAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/deck");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var decks = JsonSerializer.Deserialize<List<Deck>>(json, _jsonOptions);
                
                return decks ?? new List<Deck>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching decks: {ex.Message}");
                return new List<Deck>();
            }
        }

        public async Task<Deck?> GetDeckByIdAsync(string deckId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/deck/{deckId}");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var deck = JsonSerializer.Deserialize<Deck>(json, _jsonOptions);
                
                return deck;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching deck {deckId}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Deck>> GetDecksByTypeAsync(string deckType)
        {
            var allDecks = await GetAllDecksAsync();
            // Since Deck doesn't have DeckType, we'll determine type by card content
            return allDecks.Where(d => DeckContainsType(d, deckType)).ToList();
        }

        private bool DeckContainsType(Deck deck, string deckType)
        {
            if (deckType.Equals("Civic", StringComparison.OrdinalIgnoreCase))
            {
                return deck.Civic.Any();
            }
            else if (deckType.Equals("Military", StringComparison.OrdinalIgnoreCase))
            {
                return deck.Army.Any();
            }
            return false;
        }

        public async Task<List<Deck>> GetCivicDecksAsync()
        {
            return await GetDecksByTypeAsync("Civic");
        }

        public async Task<List<Deck>> GetMilitaryDecksAsync()
        {
            return await GetDecksByTypeAsync("Military");
        }
    }
}
