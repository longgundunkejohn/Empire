using Empire.Shared.Models;
using Empire.Shared.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

public class GameApi
{
    private readonly HttpClient _http;

    public GameApi(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<GamePreview>> GetOpenGames()
    {
        try
        {
            var response = await _http.GetAsync("api/game/open");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[GameApi] Failed to fetch open games: {response.StatusCode}");
                return new();
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<GamePreview>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GameApi] GetOpenGames() error: {ex.Message}");
            return new();
        }
    }

    public async Task<List<Card>> GetDeck(string gameId, string playerId)
    {
        try
        {
            var response = await _http.GetAsync($"api/game/deck/{gameId}/{playerId}");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[GameApi] GetDeck failed: {response.StatusCode}");
                return new();
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<Card>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GameApi] GetDeck() error: {ex.Message}");
            return new();
        }
    }

    public async Task<GameState?> GetGameState(string gameId, string playerId)
    {
        try
        {
            return await _http.GetFromJsonAsync<GameState>($"api/game/state/{gameId}/{playerId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GameApi] GetGameState() error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> JoinGame(string gameId, string playerId, List<int> civicDeck, List<int> militaryDeck)
    {
        try
        {
            var deck = new PlayerDeck
            {
                CivicDeck = civicDeck,
                MilitaryDeck = militaryDeck
            };

            var response = await _http.PostAsJsonAsync($"api/game/join/{gameId}/{playerId}", deck);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GameApi] JoinGame() error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SubmitMove(string gameId, GameMove move)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"api/game/move?gameId={gameId}", move);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GameApi] SubmitMove() error: {ex.Message}");
            return false;
        }
    }
}
