using Empire.Shared.Models;
using System.Net.Http.Json;

public class GameApi
{
    private readonly HttpClient _http;

    public GameApi(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<Card>> GetDeck(string gameId, string playerId)
    {
        var result = await _http.GetFromJsonAsync<List<Card>>($"api/game/deck/{gameId}/{playerId}");
        return result ?? new List<Card>();
    }
}
