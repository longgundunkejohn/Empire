using Empire.Shared.Models;
using Empire.Shared.Models.DTOs;
using System.Net.Http.Json;

namespace Empire.Client.Services
{
    public class GameApi
    {
        private readonly HttpClient _http;
        private readonly DeckService _deckService;

        public GameApi(HttpClient http, DeckService deckService)
        {
            _http = http;
            _deckService = deckService;
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
            try
            {
                return await _http.GetFromJsonAsync<List<PlayerDeck>>($"api/prelobby/decks/{playerName}") ?? new();
            }
            catch
            {
                // Fallback to empty list if server is unavailable
                return new List<PlayerDeck>();
            }
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

        // Empire-specific API methods
        
        public async Task<bool> CreateEmpireGame(string gameId, string player1Id, string player2Id)
        {
            var request = new { Player1Id = player1Id, Player2Id = player2Id };
            var response = await _http.PostAsJsonAsync($"api/game/{gameId}/empire/create", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SetupPlayerDeck(string gameId, string playerId, List<int> armyDeckIds, List<int> civicDeckIds)
        {
            var request = new { ArmyDeckIds = armyDeckIds, CivicDeckIds = civicDeckIds };
            var response = await _http.PostAsJsonAsync($"api/game/{gameId}/empire/setup-deck/{playerId}", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeployArmyCard(string gameId, string playerId, int cardId, int manaCost)
        {
            var request = new { PlayerId = playerId, CardId = cardId, ManaCost = manaCost };
            var response = await _http.PostAsJsonAsync($"api/game/{gameId}/empire/deploy-army", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> PlayVillager(string gameId, string playerId, int cardId)
        {
            var request = new { PlayerId = playerId, CardId = cardId };
            var response = await _http.PostAsJsonAsync($"api/game/{gameId}/empire/play-villager", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SettleTerritory(string gameId, string playerId, int cardId, string territoryId)
        {
            var request = new { PlayerId = playerId, CardId = cardId, TerritoryId = territoryId };
            var response = await _http.PostAsJsonAsync($"api/game/{gameId}/empire/settle-territory", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> CommitUnits(string gameId, string playerId, Dictionary<int, string> unitCommitments)
        {
            var request = new { PlayerId = playerId, UnitCommitments = unitCommitments };
            var response = await _http.PostAsJsonAsync($"api/game/{gameId}/empire/commit-units", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> PassInitiative(string gameId, string playerId)
        {
            var request = new { PlayerId = playerId };
            var response = await _http.PostAsJsonAsync($"api/game/{gameId}/empire/pass-initiative", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DrawCards(string gameId, string playerId, bool drawArmy = true)
        {
            var request = new { PlayerId = playerId, DrawArmy = drawArmy };
            var response = await _http.PostAsJsonAsync($"api/game/{gameId}/empire/draw-cards", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateMorale(string gameId, string playerId, int damage)
        {
            var request = new { PlayerId = playerId, Damage = damage };
            var response = await _http.PostAsJsonAsync($"api/game/{gameId}/empire/update-morale", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<GameState?> GetGameStateAsync(string gameId)
        {
            try
            {
                return await _http.GetFromJsonAsync<GameState>($"api/game/{gameId}/state");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting game state: {ex.Message}");
                return null;
            }
        }
    }
}
