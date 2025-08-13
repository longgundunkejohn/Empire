using Empire.Shared.Models;
using Empire.Shared.Models.Enums;

namespace Empire.Client.Services
{
    /// <summary>
    /// Manual game service for Cockatrice-like player-controlled gameplay
    /// Players are responsible for following rules - no server-side enforcement
    /// </summary>
    public class ManualGameService
    {
        private readonly GameHubService _hubService;
        private readonly CardDataService _cardService;
        
        public string? CurrentGameId { get; private set; }
        public string? CurrentPlayerId { get; private set; }
        public GameState? CurrentGameState { get; private set; }
        
        // Manual game events
        public event Action<GameState>? OnGameStateChanged;
        public event Action<string, string>? OnChatMessage;
        public event Action<string, int, string, string>? OnCardMoved;
        public event Action<int, bool>? OnCardExertionChanged;
        public event Action<string, int>? OnCounterChanged;

        public ManualGameService(GameHubService hubService, CardDataService cardService)
        {
            _hubService = hubService;
            _cardService = cardService;
            
            // Subscribe to hub events for manual actions
            _hubService.OnCardMoved += HandleCardMoved;
            _hubService.OnCardExertionToggled += HandleCardExertionToggled;
            _hubService.OnGameStateUpdated += HandleGameStateUpdated;
            _hubService.OnChatMessage += HandleChatMessageAsync;
        }

        public async Task InitializeGame(string gameId, string playerId)
        {
            CurrentGameId = gameId;
            CurrentPlayerId = playerId;
            
            // Connect to SignalR hub
            await _hubService.ConnectAsync(gameId);
            
            // Load game state
            await RefreshGameState();
        }

        public async Task RefreshGameState()
        {
            if (string.IsNullOrEmpty(CurrentGameId)) return;
            
            try
            {
                // Get game state from server
                var gameState = await GetGameStateFromServer();
                if (gameState != null)
                {
                    CurrentGameState = gameState;
                    OnGameStateChanged?.Invoke(gameState);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing game state: {ex.Message}");
            }
        }

        // Manual Card Operations (Cockatrice-style)

        /// <summary>
        /// Move card between zones manually - no rule validation
        /// </summary>
        public async Task MoveCardManual(int cardId, string fromZone, string toZone, int? position = null)
        {
            if (string.IsNullOrEmpty(CurrentGameId) || string.IsNullOrEmpty(CurrentPlayerId)) return;
            
            await _hubService.MoveCardManual(CurrentGameId, CurrentPlayerId, cardId, fromZone, toZone, position);
            await SendChatMessage($"moved card {cardId} from {fromZone} to {toZone}");
        }

        /// <summary>
        /// Move multiple cards at once
        /// </summary>
        public async Task MoveMultipleCardsManual(List<int> cardIds, string fromZone, string toZone)
        {
            if (string.IsNullOrEmpty(CurrentGameId) || string.IsNullOrEmpty(CurrentPlayerId)) return;
            
            await _hubService.MoveMultipleCardsManual(CurrentGameId, CurrentPlayerId, cardIds, fromZone, toZone);
            await SendChatMessage($"moved {cardIds.Count} cards from {fromZone} to {toZone}");
        }

        /// <summary>
        /// Toggle card exertion (tap/untap)
        /// </summary>
        public async Task ToggleCardExertion(int cardId)
        {
            if (string.IsNullOrEmpty(CurrentGameId) || string.IsNullOrEmpty(CurrentPlayerId)) return;
            
            await _hubService.ToggleCardExertion(CurrentGameId, CurrentPlayerId, cardId, true);
        }

        /// <summary>
        /// Draw cards from deck manually
        /// </summary>
        public async Task DrawCardsManual(string deckType, int count = 1)
        {
            if (string.IsNullOrEmpty(CurrentGameId) || string.IsNullOrEmpty(CurrentPlayerId)) return;
            
            await _hubService.DrawCardsManual(CurrentGameId, CurrentPlayerId, deckType, count);
            await SendChatMessage($"drew {count} cards from {deckType} deck");
        }

        /// <summary>
        /// Shuffle deck manually
        /// </summary>
        public async Task ShuffleDeckManual(string deckType)
        {
            if (string.IsNullOrEmpty(CurrentGameId) || string.IsNullOrEmpty(CurrentPlayerId)) return;
            
            await _hubService.ShuffleDeckManual(CurrentGameId, CurrentPlayerId, deckType);
            await SendChatMessage($"shuffled {deckType} deck");
        }

        /// <summary>
        /// Add/remove counters on cards
        /// </summary>
        public async Task ModifyCounters(int cardId, string counterType, int change)
        {
            if (string.IsNullOrEmpty(CurrentGameId) || string.IsNullOrEmpty(CurrentPlayerId)) return;
            
            // For now, just send a chat message - counters can be tracked client-side
            var action = change > 0 ? "added" : "removed";
            await SendChatMessage($"{action} {Math.Abs(change)} {counterType} counter(s) on card {cardId}");
        }

        /// <summary>
        /// Reveal cards to opponent
        /// </summary>
        public async Task RevealCards(List<int> cardIds, string reason = "")
        {
            if (string.IsNullOrEmpty(CurrentGameId) || string.IsNullOrEmpty(CurrentPlayerId)) return;
            
            var cardNames = await GetCardNames(cardIds);
            var reasonText = string.IsNullOrEmpty(reason) ? "" : $" ({reason})";
            await SendChatMessage($"reveals: {string.Join(", ", cardNames)}{reasonText}");
        }

        /// <summary>
        /// Roll dice for random effects
        /// </summary>
        public async Task RollDice(int sides = 6, int count = 1)
        {
            var random = new Random();
            var results = new List<int>();
            
            for (int i = 0; i < count; i++)
            {
                results.Add(random.Next(1, sides + 1));
            }
            
            var resultText = count == 1 ? results[0].ToString() : $"[{string.Join(", ", results)}]";
            await SendChatMessage($"rolled {resultText} on {count}d{sides}");
        }

        /// <summary>
        /// Flip a coin
        /// </summary>
        public async Task FlipCoin()
        {
            var random = new Random();
            var result = random.Next(2) == 0 ? "Heads" : "Tails";
            await SendChatMessage($"flipped a coin: {result}");
        }

        /// <summary>
        /// Send chat message
        /// </summary>
        public async Task SendChatMessage(string message)
        {
            if (string.IsNullOrEmpty(CurrentGameId) || string.IsNullOrEmpty(CurrentPlayerId)) return;
            
            await _hubService.SendChatMessage(CurrentGameId, CurrentPlayerId, message);
        }

        /// <summary>
        /// Quick shortcuts for common Empire actions
        /// </summary>
        public async Task QuickAction(string action)
        {
            switch (action.ToLower())
            {
                case "draw":
                case "draw army":
                    await DrawCardsManual("army", 1);
                    break;
                case "draw civic":
                    await DrawCardsManual("civic", 2);
                    break;
                case "pass":
                    await SendChatMessage("passes initiative");
                    break;
                case "unexert":
                case "unexert all":
                    await SendChatMessage("unexerts all cards");
                    break;
                case "new round":
                    await SendChatMessage("--- NEW ROUND ---");
                    break;
                case "strategy":
                    await SendChatMessage("--- STRATEGY PHASE ---");
                    break;
                case "battle":
                    await SendChatMessage("--- BATTLE PHASE ---");
                    break;
                case "replenishment":
                    await SendChatMessage("--- REPLENISHMENT PHASE ---");
                    break;
                default:
                    await SendChatMessage(action);
                    break;
            }
        }

        // Helper Methods

        public List<int> GetPlayerHand(string? playerId = null)
        {
            var pid = playerId ?? CurrentPlayerId;
            if (CurrentGameState == null || string.IsNullOrEmpty(pid)) return new();
            
            var armyHand = CurrentGameState.PlayerArmyHands.GetValueOrDefault(pid, new());
            var civicHand = CurrentGameState.PlayerCivicHands.GetValueOrDefault(pid, new());
            
            return armyHand.Concat(civicHand).ToList();
        }

        public List<int> GetPlayerBoard(string? playerId = null)
        {
            var pid = playerId ?? CurrentPlayerId;
            if (CurrentGameState == null || string.IsNullOrEmpty(pid)) return new();
            
            var heartland = CurrentGameState.PlayerHeartlands.GetValueOrDefault(pid, new());
            var villagers = CurrentGameState.PlayerVillagers.GetValueOrDefault(pid, new());
            
            return heartland.Concat(villagers).ToList();
        }

        public async Task<List<string>> GetCardNames(List<int> cardIds)
        {
            var names = new List<string>();
            foreach (var cardId in cardIds)
            {
                var card = await _cardService.GetCardByIdAsync(cardId);
                names.Add(card?.Name ?? $"Unknown Card ({cardId})");
            }
            return names;
        }

        private async Task<GameState?> GetGameStateFromServer()
        {
            // This would connect to the API to get current game state
            // For now, return the current state
            return CurrentGameState;
        }

        // Event Handlers

        private async Task HandleCardMoved(string playerId, int cardId, string fromZone, string toZone)
        {
            OnCardMoved?.Invoke(playerId, cardId, fromZone, toZone);
            await RefreshGameState();
        }

        private async Task HandleCardExertionToggled(string playerId, int cardId, bool isExerted)
        {
            OnCardExertionChanged?.Invoke(cardId, isExerted);
            await RefreshGameState();
        }

        private async Task HandleGameStateUpdated(string gameId)
        {
            if (gameId == CurrentGameId)
            {
                await RefreshGameState();
            }
        }

        private async Task HandleChatMessageAsync(string playerId, string message)
        {
            OnChatMessage?.Invoke(playerId, message);
            await Task.CompletedTask;
        }
    }
}