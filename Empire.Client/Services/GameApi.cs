using Empire.Shared.DTOs;
using Empire.Shared.Models;
using System.Net.Http.Json;

namespace Empire.Client.Services
{
    public class GameApi
    {
        private readonly HttpClient _http;

        public GameApi(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<GamePreview>> GetOpenGames()
        {
            return await _http.GetFromJsonAsync<List<GamePreview>>("api/game/open") ?? new();
        }

        public async Task<List<string>> GetUploadedDeckNames()
        {
            return await _http.GetFromJsonAsync<List<string>>("api/prelobby/decks") ?? new();
        }

        public async Task<List<PlayerDeck>> GetDecksForPlayer(string playerName)
        {
            return await _http.GetFromJsonAsync<List<PlayerDeck>>($"api/prelobby/decks/{playerName}") ?? new();
        }

        public async Task<string> CreateGame(string playerName, string deckId)
        {
            var request = new GameStartRequest
            {
                Player1 = playerName,
                DeckId = deckId
            };

            var response = await _http.PostAsJsonAsync("api/game/create", request);
            if (!response.IsSuccessStatusCode)
                return string.Empty;

            return await response.Content.ReadAsStringAsync();
        }


        public async Task<bool> JoinGame(string gameId, string playerName, List<int> civicDeck, List<int> militaryDeck)
        {
            var request = new JoinGameRequest { CivicDeck = civicDeck, MilitaryDeck = militaryDeck };
            var response = await _http.PostAsJsonAsync($"api/game/{gameId}/join/{playerName}", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<GameState?> GetGameState(string gameId)
        {
            return await _http.GetFromJsonAsync<GameState>($"api/game/{gameId}/state");
        }

        public async Task<bool> SubmitMove(string gameId, GameMove move)
        {
            var response = await _http.PostAsJsonAsync($"api/game/{gameId}/move", move);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DrawCard(string gameId, string playerId, string type)
        {
            var response = await _http.PostAsync($"api/game/{gameId}/draw/{playerId}/{type}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UploadDeck(string playerName, string deckName, Stream fileStream, string fileName)
        {
            var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");

            content.Add(fileContent, "file", fileName);
            content.Add(new StringContent(playerName), "playerName");
            content.Add(new StringContent(deckName), "deckName");

            var response = await _http.PostAsync($"api/prelobby/upload?playerName={Uri.EscapeDataString(playerName)}&deckName={Uri.EscapeDataString(deckName)}", content);

            return response.IsSuccessStatusCode;
        }
    }
}
