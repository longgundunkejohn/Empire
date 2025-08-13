using Empire.Shared.Models;
using System.Text.Json;

namespace Empire.Client.Services
{
    public class UserDeckService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthService _authService;
        private List<UserDeck>? _userDecks;

        public UserDeckService(HttpClient httpClient, AuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
        }

        public async Task<List<UserDeck>> GetUserDecksAsync()
        {
            if (!await _authService.IsAuthenticatedAsync())
                return new List<UserDeck>();

            try
            {
                var response = await _httpClient.GetAsync("/api/deck/user-decks");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _userDecks = JsonSerializer.Deserialize<List<UserDeck>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<UserDeck>();
                    
                    return _userDecks;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user decks: {ex.Message}");
            }

            return new List<UserDeck>();
        }

        public async Task<bool> SaveDeckAsync(string deckName, List<int> armyCards, List<int> civicCards)
        {
            if (!await _authService.IsAuthenticatedAsync())
                return false;

            try
            {
                var deckData = new
                {
                    Name = deckName,
                    ArmyCards = armyCards,
                    CivicCards = civicCards
                };

                var json = JsonSerializer.Serialize(deckData);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/deck/save", content);
                
                if (response.IsSuccessStatusCode)
                {
                    // Refresh cached decks
                    _userDecks = null;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving deck: {ex.Message}");
            }

            return false;
        }

        public async Task<bool> DeleteDeckAsync(int deckId)
        {
            if (!await _authService.IsAuthenticatedAsync())
                return false;

            try
            {
                var response = await _httpClient.DeleteAsync($"/api/deck/{deckId}");
                
                if (response.IsSuccessStatusCode)
                {
                    // Refresh cached decks
                    _userDecks = null;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting deck: {ex.Message}");
            }

            return false;
        }

        public async Task<UserDeck?> GetDeckAsync(int deckId)
        {
            var decks = await GetUserDecksAsync();
            return decks.FirstOrDefault(d => d.Id == deckId);
        }

        public async Task<bool> ValidateDeckAsync(UserDeck deck)
        {
            try
            {
                var armyCards = JsonSerializer.Deserialize<List<int>>(deck.ArmyCards) ?? new List<int>();
                var civicCards = JsonSerializer.Deserialize<List<int>>(deck.CivicCards) ?? new List<int>();

                // Empire TCG deck validation
                var totalCards = armyCards.Count + civicCards.Count;
                var isValidSize = totalCards >= 45 && totalCards <= 60;
                var hasEnoughArmy = armyCards.Count >= 30;
                var hasEnoughCivic = civicCards.Count >= 15;

                return isValidSize && hasEnoughArmy && hasEnoughCivic;
            }
            catch
            {
                return false;
            }
        }

        public async Task<DeckStats> GetDeckStatsAsync(UserDeck deck)
        {
            try
            {
                var armyCards = JsonSerializer.Deserialize<List<int>>(deck.ArmyCards) ?? new List<int>();
                var civicCards = JsonSerializer.Deserialize<List<int>>(deck.CivicCards) ?? new List<int>();

                return new DeckStats
                {
                    TotalCards = armyCards.Count + civicCards.Count,
                    ArmyCards = armyCards.Count,
                    CivicCards = civicCards.Count,
                    IsValid = await ValidateDeckAsync(deck)
                };
            }
            catch
            {
                return new DeckStats();
            }
        }
    }

    public class DeckStats
    {
        public int TotalCards { get; set; }
        public int ArmyCards { get; set; }
        public int CivicCards { get; set; }
        public bool IsValid { get; set; }
    }
}