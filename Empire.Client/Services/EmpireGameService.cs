using Empire.Shared.Models;
using Empire.Shared.Models.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Empire.Client.Services
{
    public class EmpireGameService
    {
        private readonly GameHubService _hubService;
        private readonly GameApi _gameApi;
        
        public string? CurrentGameId { get; private set; }
        public string? CurrentPlayerId { get; private set; }
        public GameState? CurrentGameState { get; private set; }
        
        // Empire-specific events
        public event Action<GamePhase, string>? OnPhaseChanged;
        public event Action<string>? OnInitiativeChanged;
        public event Action<string, int>? OnMoraleChanged;
        public event Action<string, int>? OnTierChanged;
        public event Action<string>? OnGameWon;

        public EmpireGameService(GameHubService hubService, GameApi gameApi)
        {
            _hubService = hubService;
            _gameApi = gameApi;
            
            // Subscribe to hub events
            _hubService.OnActionTaken += HandleActionTaken;
            _hubService.OnInitiativePassed += HandleInitiativePassed;
            _hubService.OnPlayerPassed += HandlePlayerPassed;
            _hubService.OnPhaseTransition += HandlePhaseTransition;
            _hubService.OnMoraleUpdated += HandleMoraleUpdated;
            _hubService.OnGameStateUpdated += HandleGameStateUpdated;
        }

        public async Task InitializeGame(string gameId, string playerId)
        {
            CurrentGameId = gameId;
            CurrentPlayerId = playerId;
            
            // Load current game state
            await RefreshGameState();
            
            // Connect to SignalR hub
            await _hubService.ConnectAsync(gameId);
        }

        public async Task RefreshGameState()
        {
            if (string.IsNullOrEmpty(CurrentGameId)) return;
            
            try
            {
                CurrentGameState = await _gameApi.GetGameStateAsync(CurrentGameId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing game state: {ex.Message}");
            }
        }

        // Empire Actions
        
        /// <summary>
        /// Deploy an army card from hand to heartland (exerted)
        /// </summary>
        public async Task<bool> DeployArmyCard(int cardId)
        {
            if (!CanTakeAction()) return false;
            
            var player = GetCurrentPlayer();
            if (player == null) return false;
            
            var card = GetCardById(cardId);
            if (card == null || card.Type != "Army") return false;
            
            // For now, use simplified mana cost of 1
            // TODO: Implement proper mana cost calculation based on card and player tier
            int manaCost = 1;
            
            try
            {
                await _hubService.DeployArmyCard(CurrentGameId!, CurrentPlayerId!, cardId, manaCost);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deploying army card: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Play a villager from civic hand to heartland (once per round)
        /// </summary>
        public async Task<bool> PlayVillager(int cardId)
        {
            if (!CanTakeAction()) return false;
            
            var player = GetCurrentPlayer();
            if (player == null || player.HasPlayedVillagerThisRound) return false;
            
            var card = GetCardById(cardId);
            if (card == null || card.Type != "Villager") return false;
            
            await _hubService.PlayVillager(CurrentGameId!, CurrentPlayerId!, cardId);
            return true;
        }

        /// <summary>
        /// Settle a territory with a civic card (once per round)
        /// </summary>
        public async Task<bool> SettleTerritory(int cardId, string territoryId)
        {
            if (!CanTakeAction()) return false;
            
            var player = GetCurrentPlayer();
            if (player == null || player.HasSettledThisRound) return false;
            
            // Must be occupying the territory to settle it
            if (!player.IsOccupyingTerritory(territoryId)) return false;
            
            var card = GetCardById(cardId);
            if (card == null || !card.IsCivicCard()) return false;
            
            await _hubService.SettleTerritory(CurrentGameId!, CurrentPlayerId!, cardId, territoryId);
            return true;
        }

        /// <summary>
        /// Commit units from heartland to territories (once per round)
        /// </summary>
        public async Task<bool> CommitUnits(Dictionary<int, string> unitCommitments)
        {
            if (!CanTakeAction()) return false;
            
            var player = GetCurrentPlayer();
            if (player == null || player.HasCommittedThisRound) return false;
            
            await _hubService.CommitUnits(CurrentGameId!, CurrentPlayerId!, unitCommitments);
            return true;
        }

        /// <summary>
        /// Toggle card exertion (double-click)
        /// </summary>
        public async Task ToggleCardExertion(int cardId)
        {
            var card = GetCardById(cardId);
            if (card == null) return;
            
            bool newExertionState = !card.IsExerted;
            await _hubService.ToggleCardExertion(CurrentGameId!, CurrentPlayerId!, cardId, newExertionState);
        }

        /// <summary>
        /// Move card between zones (drag and drop)
        /// </summary>
        public async Task MoveCard(int cardId, string fromZone, string toZone)
        {
            await _hubService.MoveCard(CurrentGameId!, CurrentPlayerId!, cardId, fromZone, toZone);
        }

        /// <summary>
        /// Pass initiative to opponent
        /// </summary>
        public async Task PassInitiative()
        {
            if (!CanTakeAction()) return;
            
            await _hubService.PassInitiative(CurrentGameId!, CurrentPlayerId!);
        }

        /// <summary>
        /// Unexert all cards (replenishment phase)
        /// </summary>
        public async Task UnexertAllCards()
        {
            await _hubService.UnexertAllCards(CurrentGameId!, CurrentPlayerId!);
        }

        // Helper Methods
        
        public bool CanTakeAction()
        {
            return CurrentGameState?.InitiativeHolder == CurrentPlayerId;
        }

        public bool IsMyTurn()
        {
            return CanTakeAction();
        }

        public GamePhase GetCurrentPhase()
        {
            return CurrentGameState?.CurrentPhase ?? GamePhase.Strategy;
        }

        public string GetCurrentPhaseString()
        {
            return GetCurrentPhase().ToString();
        }

        public EmpirePlayer? GetCurrentPlayer()
        {
            if (CurrentGameState == null || CurrentPlayerId == null) return null;
            
            // This would need to be implemented based on how EmpirePlayer is stored in GameState
            // For now, return null - this will be implemented when we integrate EmpirePlayer with GameState
            return null;
        }

        public Card? GetCardById(int cardId)
        {
            // This would need to be implemented based on how cards are stored
            // For now, return null - this will be implemented when we have card storage
            return null;
        }

        public int GetPlayerMorale(string playerId)
        {
            return CurrentGameState?.PlayerMorale.GetValueOrDefault(playerId, 25) ?? 25;
        }

        public int GetPlayerTier(string playerId)
        {
            return CurrentGameState?.PlayerTiers.GetValueOrDefault(playerId, 1) ?? 1;
        }

        public List<string> GetOccupiedTerritories(string playerId)
        {
            if (CurrentGameState == null) return new List<string>();
            
            return CurrentGameState.TerritoryOccupants
                .Where(kvp => kvp.Value == playerId)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        public bool IsGameOver()
        {
            if (CurrentGameState == null) return false;
            
            return CurrentGameState.PlayerMorale.Values.Any(morale => morale <= 0);
        }

        public string? GetWinner()
        {
            if (CurrentGameState == null || !IsGameOver()) return null;
            
            var loser = CurrentGameState.PlayerMorale.FirstOrDefault(kvp => kvp.Value <= 0);
            if (loser.Key == null) return null;
            
            // Winner is the other player
            return CurrentGameState.PlayerMorale.Keys.FirstOrDefault(p => p != loser.Key);
        }

        // Event Handlers
        
        private async Task HandleActionTaken(string playerId, string actionType, object actionData)
        {
            Console.WriteLine($"Action taken: {actionType} by {playerId}");
            await RefreshGameState();
        }

        private async Task HandleInitiativePassed(string playerId)
        {
            Console.WriteLine($"Initiative passed from {playerId}");
            OnInitiativeChanged?.Invoke(GetOpponentId(playerId));
            await RefreshGameState();
        }

        private async Task HandlePlayerPassed(string playerId)
        {
            Console.WriteLine($"Player {playerId} passed");
            await RefreshGameState();
        }

        private async Task HandlePhaseTransition(string newPhase, string initiativeHolder)
        {
            Console.WriteLine($"Phase transition to {newPhase}, initiative to {initiativeHolder}");
            
            if (Enum.TryParse<GamePhase>(newPhase, out var phase))
            {
                OnPhaseChanged?.Invoke(phase, initiativeHolder);
            }
            
            OnInitiativeChanged?.Invoke(initiativeHolder);
            await RefreshGameState();
        }

        private async Task HandleMoraleUpdated(string playerId, int newMorale, int damage)
        {
            Console.WriteLine($"Morale updated for {playerId}: {newMorale} (-{damage})");
            OnMoraleChanged?.Invoke(playerId, newMorale);
            
            if (newMorale <= 0)
            {
                OnGameWon?.Invoke(GetOpponentId(playerId));
            }
            
            await RefreshGameState();
        }

        private async Task HandleGameStateUpdated(string gameId)
        {
            if (gameId == CurrentGameId)
            {
                await RefreshGameState();
            }
        }

        private string GetOpponentId(string playerId)
        {
            if (CurrentGameState == null) return "";
            
            return playerId == CurrentGameState.Player1 ? 
                CurrentGameState.Player2 ?? "" : 
                CurrentGameState.Player1;
        }
    }
}
