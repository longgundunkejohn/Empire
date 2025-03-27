using Empire.Shared.Models;
using System.Net.Http.Json;

using Empire.Shared.DTOs; // ⬅ Make sure this using is present

public class GameApi
{
    private readonly HttpClient _http;

    public GameApi(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<GamePreview>> GetOpenGames()
    {
        var games = await _http.GetFromJsonAsync<List<GamePreview>>("api/game/open");
        return games ?? new List<GamePreview>();
    }

    // you already likely have this one:
    public async Task<List<Card>> GetDeck(string gameId, string playerId)
    {
        var result = await _http.GetFromJsonAsync<List<Card>>($"api/game/deck/{gameId}/{playerId}");
        return result ?? new List<Card>();
    }
}