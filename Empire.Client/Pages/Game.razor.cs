using Empire.Client.Services;
using Empire.Shared.Models;
using Empire.Shared.Models.DTOs;
using Empire.Shared.Models.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Empire.Client.Pages
{
    public partial class Game : ComponentBase, IDisposable
    {
        [Parameter] public string gameId { get; set; } = string.Empty;
        [Parameter] public string playerId { get; set; } = string.Empty;

        [Inject] public GameApi GameApi { get; set; } = default!;
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        [Inject] public GameHubService HubService { get; set; } = default!;
        [Inject] public GameStateClientService GameStateService { get; set; } = default!;
        [Inject] public CardService CardService { get; set; } = default!;
        [Inject] public DeckService DeckService { get; set; } = default!;

        private bool isLoading = true;
        private string errorMessage = string.Empty;
        private bool isDragging = false;
        private CardData? ZoomedCard = null;
        private string PreviewXpx = "0px";
        private string PreviewYpx = "0px";
        private int? draggedCardId = null;
        private string chatInput = string.Empty;
        private List<(string PlayerId, string Message)> ChatLog = new();
        private List<CardData> allCards = new();

        // Properties that get data from the state service
        private GameState? gameState => GameStateService.CurrentGameState;
        private List<int> PlayerHand => GameStateService.GetPlayerHandIds(playerId);
        private List<BoardCard> PlayerBoard => GameStateService.GetPlayerBoard(playerId);
        private List<BoardCard> OpponentBoard => GameStateService.GetPlayerBoard(GameStateService.GetOpponentId(playerId) ?? "");
        private int CivicDeckCount => GameStateService.GetDeckCount(playerId, "civic");
        private int MilitaryDeckCount => GameStateService.GetDeckCount(playerId, "military");
        private int PlayerLifeTotal => GameStateService.GetPlayerLifeTotal(playerId);
        private int OpponentLifeTotal => GameStateService.GetPlayerLifeTotal(GameStateService.GetOpponentId(playerId) ?? "");
        private string CurrentPhase => GameStateService.GetCurrentPhase();
        private bool IsMyTurn => GameStateService.IsPlayerTurn(playerId);

        protected override async Task OnInitializedAsync()
        {
            try
            {
                isLoading = true;
                errorMessage = string.Empty;

                // Load all cards from the server
                allCards = await CardService.GetAllCardsAsync();
                ChatLog.Add(("System", $"📚 Loaded {allCards.Count} cards from server"));

                // Initialize with real data or fallback to mock
                await InitializeGameState();

                // Load initial game state
                await RefreshGameState();

                // Connect to SignalR hub
                await HubService.ConnectAsync(gameId);

                // Subscribe to SignalR events
                HubService.OnBoardUpdate += HandleBoardUpdate;
                HubService.OnGameStateUpdated += HandleGameStateUpdated;
                HubService.OnChatMessage += HandleChatMessage;
                HubService.OnMoveSubmitted += HandleMoveSubmitted;
                HubService.OnPhaseChanged += HandlePhaseChanged;
                HubService.OnCardDrawn += HandleCardDrawn;
                HubService.OnCardPlayed += HandleCardPlayed;
                HubService.OnPlayerJoined += HandlePlayerJoined;
                HubService.OnGameStarted += HandleGameStarted;

                // Subscribe to state changes
                GameStateService.OnStateChanged += StateHasChanged;

                isLoading = false;
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to initialize game: {ex.Message}";
                isLoading = false;
            }
        }

        private async Task InitializeGameState()
        {
            if (allCards.Any())
            {
                // Create a game state with real cards
                var militaryCards = allCards.Where(c => c.CardType is "Unit" or "Tactic" or "Battle Tactic" or "Chronicle" or "Skirmisher").ToList();
                var civicCards = allCards.Where(c => c.CardType is "Settlement" or "Villager").ToList();

                var mockState = new GameState
                {
                    GameId = gameId,
                    CurrentPhase = GamePhase.Strategy,
                    Player1 = playerId,
                    Player2 = "opponent",
                    InitiativeHolder = playerId,
                    PriorityPlayer = playerId,
                    PlayerHands = new Dictionary<string, List<int>>
                    {
                        [playerId] = militaryCards.Take(7).Select(c => c.CardID).ToList(),
                        ["opponent"] = militaryCards.Skip(7).Take(7).Select(c => c.CardID).ToList()
                    },
                    PlayerBoard = new Dictionary<string, List<BoardCard>>
                    {
                        [playerId] = new List<BoardCard>(),
                        ["opponent"] = new List<BoardCard>()
                    },
                    PlayerLifeTotals = new Dictionary<string, int>
                    {
                        [playerId] = 20,
                        ["opponent"] = 20
                    }
                };

                GameStateService.UpdateGameState(mockState);
                ChatLog.Add(("System", "🎮 Initialized with real card data"));
            }
            else
            {
                ChatLog.Add(("System", "⚠️ No cards loaded, using fallback data"));
            }
        }


        // SignalR Event Handlers
        private async Task HandleBoardUpdate(BoardPositionUpdate update)
        {
            if (update.GameId == gameId)
            {
                await RefreshGameState();
            }
        }

        private async Task HandleGameStateUpdated(string updatedGameId)
        {
            if (updatedGameId == gameId)
            {
                await RefreshGameState();
            }
        }

        private async Task HandleChatMessage(string senderId, string message)
        {
            ChatLog.Add((senderId, message));
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleMoveSubmitted(GameMove move)
        {
            ChatLog.Add((move.PlayerId, $"🎯 {move.MoveType}"));
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandlePhaseChanged(string newPhase, string activePlayer)
        {
            ChatLog.Add(("System", $"⏰ Phase changed to {newPhase}. Active player: {activePlayer}"));
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleCardDrawn(string drawingPlayerId, int cardId)
        {
            if (drawingPlayerId != playerId)
            {
                ChatLog.Add((drawingPlayerId, "🃏 Drew a card"));
            }
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleCardPlayed(string playingPlayerId, int cardId)
        {
            var card = allCards.FirstOrDefault(c => c.CardID == cardId);
            var cardName = card?.Name ?? $"Card #{cardId}";
            ChatLog.Add((playingPlayerId, $"🎴 Played {cardName}"));
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandlePlayerJoined(string joinedPlayerId)
        {
            ChatLog.Add(("System", $"👋 {joinedPlayerId} joined the game"));
            await RefreshGameState();
        }

        private async Task HandleGameStarted(string startedGameId)
        {
            if (startedGameId == gameId)
            {
                ChatLog.Add(("System", "🚀 Game started!"));
                await RefreshGameState();
            }
        }

        // Game Actions
        private async Task DrawCard(string type)
        {
            try
            {
                if (!IsMyTurn)
                {
                    ChatLog.Add(("System", "❌ It's not your turn!"));
                    return;
                }

                // Draw a card from real data
                var availableCards = type == "civic" 
                    ? allCards.Where(c => c.CardType is "Settlement" or "Villager").ToList()
                    : allCards.Where(c => c.CardType is "Unit" or "Tactic" or "Battle Tactic" or "Chronicle" or "Skirmisher").ToList();
                
                if (availableCards.Any())
                {
                    var random = new Random();
                    var drawnCard = availableCards[random.Next(availableCards.Count)];
                    
                    // Add to hand
                    GameStateService.AddCardToHand(playerId, drawnCard.CardID);
                    
                    ChatLog.Add((playerId, $"🃏 Drew {drawnCard.Name}"));
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (Exception ex)
            {
                ChatLog.Add(("System", $"❌ Error drawing card: {ex.Message}"));
            }
        }

        private async Task DrawCivic() => await DrawCard("civic");
        private async Task DrawMilitary() => await DrawCard("military");

        private void OnDragStart(int cardId) => draggedCardId = cardId;

        private async Task OnCardDrop()
        {
            if (!draggedCardId.HasValue || !IsMyTurn) return;

            try
            {
                // Move card from hand to board
                GameStateService.PlayCardFromHand(playerId, draggedCardId.Value);
                
                var card = allCards.FirstOrDefault(c => c.CardID == draggedCardId.Value);
                ChatLog.Add((playerId, $"🎴 Played {card?.Name ?? $"Card #{draggedCardId.Value}"}"));
                
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                ChatLog.Add(("System", $"❌ Error playing card: {ex.Message}"));
            }
            finally
            {
                draggedCardId = null;
                isDragging = false;
            }
        }

        // Card click handlers for the new component structure
        private async Task HandleCardClick(int cardId)
        {
            var card = allCards.FirstOrDefault(c => c.CardID == cardId);
            Console.WriteLine($"Clicked: {card?.Name ?? "Unknown"} (#{cardId})");
            
            // If it's a board card, try to exert it
            if (PlayerBoard.Any(bc => bc.CardId == cardId) && IsMyTurn)
            {
                GameStateService.ExertCard(playerId, cardId);
                ChatLog.Add((playerId, $"⚡ Exerted {card?.Name ?? $"Card #{cardId}"}"));
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task HandleCardDoubleClick(int cardId)
        {
            var card = allCards.FirstOrDefault(c => c.CardID == cardId);
            Console.WriteLine($"Double-clicked: {card?.Name ?? "Unknown"} (#{cardId})");
            await Task.CompletedTask;
        }

        private async Task HandleHandCardClick(int cardId)
        {
            var card = allCards.FirstOrDefault(c => c.CardID == cardId);
            Console.WriteLine($"Hand card clicked: {card?.Name ?? "Unknown"} (#{cardId})");
            await Task.CompletedTask;
        }

        private async Task HandleHandCardDoubleClick(int cardId)
        {
            // Double-click to play card from hand
            if (IsMyTurn)
            {
                GameStateService.PlayCardFromHand(playerId, cardId);
                var card = allCards.FirstOrDefault(c => c.CardID == cardId);
                ChatLog.Add((playerId, $"🎴 Played {card?.Name ?? $"Card #{cardId}"}"));
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task HandleOpponentCardClick(int cardId)
        {
            var card = allCards.FirstOrDefault(c => c.CardID == cardId);
            Console.WriteLine($"Opponent card clicked: {card?.Name ?? "Unknown"} (#{cardId})");
            await Task.CompletedTask;
        }

        private async Task HandleOpponentCardDoubleClick(int cardId)
        {
            var card = allCards.FirstOrDefault(c => c.CardID == cardId);
            Console.WriteLine($"Opponent card double-clicked: {card?.Name ?? "Unknown"} (#{cardId})");
            await Task.CompletedTask;
        }

        private void ShowZoomedCard(CardData card, MouseEventArgs e)
        {
            const int previewWidth = 300, previewHeight = 420, screenWidth = 1920, screenHeight = 1080;
            int offsetX = (e.ClientX + previewWidth + 30 > screenWidth) ? -previewWidth - 20 : 20;
            int offsetY = (e.ClientY + previewHeight + 30 > screenHeight) ? -previewHeight - 20 : 20;
            PreviewXpx = $"{e.ClientX + offsetX}px";
            PreviewYpx = $"{e.ClientY + offsetY}px";
            ZoomedCard = card;
        }

        private void HideZoomedCard() => ZoomedCard = null;

        private string GetCardType(int cardId)
        {
            var card = allCards.FirstOrDefault(c => c.CardID == cardId);
            return card?.CardType ?? "Unknown";
        }

        private string GetCardImagePath(CardData card)
        {
            if (card?.ImageFileName != null && !string.IsNullOrEmpty(card.ImageFileName))
            {
                return $"/images/cards/{card.ImageFileName}";
            }
            return "/images/card-placeholder.png";
        }

        private async Task HandleChatKey(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(chatInput))
            {
                var message = chatInput.Trim();
                if (message.StartsWith("/"))
                {
                    await HandleChatCommand(message);
                }
                else
                {
                    ChatLog.Add((playerId, message));
                    await InvokeAsync(StateHasChanged);
                }
                chatInput = string.Empty;
            }
        }

        private async Task HandleChatCommand(string command)
        {
            switch (command.ToLowerInvariant())
            {
                case "/shuffle":
                    ChatLog.Add((playerId, "🔀 shuffled their deck."));
                    break;
                case "/endturn":
                    if (IsMyTurn)
                    {
                        GameStateService.EndTurn();
                        ChatLog.Add((playerId, "⏭️ ended their turn."));
                        await InvokeAsync(StateHasChanged);
                    }
                    else
                    {
                        ChatLog.Add(("System", "❌ It's not your turn!"));
                    }
                    break;
                case "/draw":
                    await DrawMilitary();
                    break;
                case "/drawcivic":
                    await DrawCivic();
                    break;
                default:
                    ChatLog.Add((playerId, $"❓ Unknown command: {command}"));
                    break;
            }
            await InvokeAsync(StateHasChanged);
        }

        private async Task RefreshGameState()
        {
            await LoadGameState();
        }

        private async Task LoadGameState()
        {
            try
            {
                var state = await GameApi.GetGameState(gameId);
                if (state != null)
                {
                    GameStateService.UpdateGameState(state);
                }
                else
                {
                    // Keep using mock data if server is unavailable
                    ChatLog.Add(("System", "⚠️ Using mock data - server unavailable"));
                }
            }
            catch (Exception ex)
            {
                ChatLog.Add(("System", $"⚠️ Server error: {ex.Message}. Using mock data."));
            }
        }

        private void NavigateToLobby()
        {
            NavigationManager.NavigateTo("/lobby");
        }

        private async Task EndTurn()
        {
            await HandleChatCommand("/endturn");
        }

        private async Task ShuffleDeck()
        {
            await HandleChatCommand("/shuffle");
        }

        public void Dispose()
        {
            // Unsubscribe from events
            if (HubService != null)
            {
                HubService.OnBoardUpdate -= HandleBoardUpdate;
                HubService.OnGameStateUpdated -= HandleGameStateUpdated;
                HubService.OnChatMessage -= HandleChatMessage;
                HubService.OnMoveSubmitted -= HandleMoveSubmitted;
                HubService.OnPhaseChanged -= HandlePhaseChanged;
                HubService.OnCardDrawn -= HandleCardDrawn;
                HubService.OnCardPlayed -= HandleCardPlayed;
                HubService.OnPlayerJoined -= HandlePlayerJoined;
                HubService.OnGameStarted -= HandleGameStarted;
            }

            if (GameStateService != null)
            {
                GameStateService.OnStateChanged -= StateHasChanged;
            }
        }
    }
}
