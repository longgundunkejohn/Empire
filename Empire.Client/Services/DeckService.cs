using Empire.Shared.Models;
using System.Text.Json;

namespace Empire.Client.Services
{
    public class DeckService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthService _authService;

        public DeckService(HttpClient httpClient, AuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
        }

        public async Task<List<UserDeck>> GetUserDecksAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/deck/user-decks");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<UserDeck>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<UserDeck>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user decks: {ex.Message}");
            }

            return new List<UserDeck>();
        }

        public async Task<bool> SaveDeckAsync(string username, string deckName, List<int> armyCards, List<int> civicCards)
        {
            try
            {
                var request = new
                {
                    Name = deckName,
                    ArmyCards = armyCards,
                    CivicCards = civicCards
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/deck/save", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving deck: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteDeckAsync(int deckId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/deck/{deckId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting deck: {ex.Message}");
                return false;
            }
        }

        public async Task<UserDeck?> GetDeckAsync(int deckId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/deck/{deckId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<UserDeck>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting deck: {ex.Message}");
            }

            return null;
        }

        public async Task<List<Card>> GetDeckCards(List<int> cardIds)
        {
            try
            {
                var request = new { CardIds = cardIds };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/deck/get-cards", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Card>>(responseJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<Card>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting deck cards: {ex.Message}");
            }

            return new List<Card>();
        }

        public async Task<bool> ValidateDeckAsync(string deckName)
        {
            try
            {
                var request = new { DeckName = deckName };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/deck/validate", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<dynamic>(responseJson);
                    return result?.GetProperty("isValid").GetBoolean() ?? false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating deck: {ex.Message}");
            }

            return false;
        }
    }
}
