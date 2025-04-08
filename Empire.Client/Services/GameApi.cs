using Empire.Shared.Models;
using Empire.Shared.DTOs;
using System.Net.Http.Json;
using System.Text.Json;
using Empire.Shared.Models.DTOs;

public class GameApi
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public GameApi(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<GamePreview>> GetOpenGames()
    {
        try
        {
            var response = await _http.GetAsync("api/game/open");
            var content = await response.Content.ReadAsStringAsync();

            Console.WriteLine("[GameApi] Raw response:");
            Console.WriteLine(string.IsNullOrWhiteSpace(content) ? "<empty>" : content);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[GameApi] ❌ Failed to get open games: {response.StatusCode}");
                return new();
            }

            var result = JsonSerializer.Deserialize<List<GamePreview>>(content, _jsonOptions);
            return result ?? new();
        }
        catch (JsonException je)
        {
            Console.WriteLine($"[GameApi] 🧨 JSON parse error: {je.Message}");
            return new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GameApi] ❌ Unexpected error in GetOpenGames: {ex.Message}");
            return new();
        }
    }

    public async Task<List<Card>> GetDeck(string gameId, string playerId)
    {
        try
        {
            var response = await _http.GetAsync($"api/game/deck/{gameId}/{playerId}");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[GameApi] ❌ GetDeck failed: {response.StatusCode}");
                Console.WriteLine(content);
                return new();
            }

            var result = JsonSerializer.Deserialize<List<Card>>(content, _jsonOptions);
            return result ?? new();
        }
        catch (JsonException je)
        {
            Console.WriteLine($"[GameApi] 🧨 JSON parse error in GetDeck: {je.Message}");
            return new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GameApi] ❌ Unexpected error in GetDeck: {ex.Message}");
            return new();
        }
    }

    public async Task<GameState?> GetGameState(string gameId, string playerId)
    {
        try
        {
            return await _http.GetFromJsonAsync<GameState>($"api/game/state/{gameId}/{playerId}", _jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GameApi] ❌ GetGameState() error: {ex.Message}");
            return null;
        }
    }
    public async Task<List<string>> GetUploadedDeckNames()
    {
        var response = await _http.GetAsync("api/prelobby/decks");

        if (!response.IsSuccessStatusCode)
            return new List<string>();

        var names = await response.Content.ReadFromJsonAsync<List<string>>();
        return names ?? new List<string>();
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
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[GameApi] ❌ JoinGame failed: {response.StatusCode} - {error}");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GameApi] ❌ JoinGame() error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SubmitMove(string gameId, GameMove move)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"api/game/move?gameId={gameId}", move);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[GameApi] ❌ SubmitMove failed: {response.StatusCode} - {error}");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GameApi] ❌ SubmitMove() error: {ex.Message}");
            return false;
        }
    }

    public async Task<string?> CreateGame(string player1, List<RawDeckEntry> player1Deck)
    {
        try
        {
            var request = new GameStartRequest
            {
                Player1 = player1,
            };

            var response = await _http.PostAsJsonAsync("api/game/create", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[GameApi] ❌ CreateGame failed: {response.StatusCode} - {error}");
                return null; // Or throw an exception if you prefer
            }

            var gameId = await response.Content.ReadAsStringAsync();
            return gameId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GameApi] ❌ CreateGame() error: {ex.Message}");
            return null; // Or throw an exception
        }
    }
}