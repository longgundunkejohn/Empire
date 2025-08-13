using Empire.Shared.Models;
using System.Text.Json;

namespace Empire.Client.Services
{
    public class LobbyService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthService _authService;

        public LobbyService(HttpClient httpClient, AuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
        }

        public async Task<List<LobbyListItem>> GetLobbyListAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/lobby/list");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<LobbyListItem>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<LobbyListItem>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading lobby list: {ex.Message}");
            }

            return new List<LobbyListItem>();
        }

        public async Task<GameLobby?> CreateLobbyAsync(CreateLobbyRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/lobby/create", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<GameLobby>(responseJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating lobby: {ex.Message}");
            }

            return null;
        }

        public async Task<bool> JoinLobbyAsync(string lobbyId)
        {
            try
            {
                var request = new { LobbyId = lobbyId };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/lobby/join", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error joining lobby: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> LeaveLobbyAsync(string lobbyId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/api/lobby/{lobbyId}/leave", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error leaving lobby: {ex.Message}");
                return false;
            }
        }

        public async Task<GameLobby?> GetLobbyAsync(string lobbyId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/lobby/{lobbyId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<GameLobby>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting lobby: {ex.Message}");
            }

            return null;
        }

        public async Task<bool> SetPlayerReadyAsync(string lobbyId, bool ready)
        {
            try
            {
                var request = new { LobbyId = lobbyId, Ready = ready };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/lobby/set-ready", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting ready status: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> StartGameAsync(string lobbyId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/api/lobby/{lobbyId}/start", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting game: {ex.Message}");
                return false;
            }
        }
    }
}