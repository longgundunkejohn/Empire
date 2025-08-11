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
        [Inject] public EmpireGameService EmpireGameService { get; set; } = default!;
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

        // Empire-specific properties
        private GameState? gameState => EmpireGameService.CurrentGameState;
        private List<int> PlayerArmyHand => gameState?.PlayerArmyHands.GetValueOrDefault(playerId, new()) ?? new();
        private List<int> PlayerCivicHand => gameState?.PlayerCivicHands.GetValueOrDefault(playerId, new()) ?? new();
        private List<int> PlayerHand => PlayerArmyHand.Concat(PlayerCivicHand).ToList();
        private List<int> PlayerHeartland => gameState?.PlayerHeartlands.GetValueOrDefault(playerId, new()) ?? new();
        private List<int> PlayerVillagers => gameState?.PlayerVillagers.GetValueOrDefault(playerId, new()) ?? new();
        
        // Legacy compatibility - convert to BoardCard format
        private List<BoardCard> PlayerBoard => PlayerHeartland.Concat(PlayerVillagers)
            .Select(cardId => new BoardCard(cardId)).ToList();
        private List<BoardCard> OpponentBoard => GetOpponentBoard();
        
        private int CivicDeckCount => gameState?.PlayerCivicDecks.GetValueOrDefault(playerId, new())?.Count ?? 0;
        private int MilitaryDeckCount => gameState?.PlayerArmyDecks.GetValueOrDefault(playerId, new())?.Count ?? 0;
        private int PlayerLifeTotal => EmpireGameService.GetPlayerMorale(playerId);
        private int OpponentLifeTotal => EmpireGameService.GetPlayerMorale(GetOpponentId());
        private string CurrentPhase => EmpireGameService.GetCurrentPhaseString();
        private bool IsMyTurn => EmpireGameService.IsMyTurn();

        protected override async Task OnInitializedAsync()
        {
            try
            {
                isLoading = true;
                errorMessage = string.Empty;

                // Load all cards from the server
                allCards = await CardService.GetAllCardsAsync();
                ChatLog.Add(("System", $"📚 Loaded {allCards.Count} cards from server"));

                // Initialize Empire game
                await EmpireGameService.InitializeGame(gameId, playerId);

                // Subscribe to Empire events
                EmpireGameService.OnPhaseChanged += HandleEmpirePhaseChanged;
                EmpireGameService.OnInitiativeChanged += HandleInitiativeChanged;
                EmpireGameService.OnMoraleChanged += HandleMoraleChanged;
                EmpireGameService.OnGameWon += HandleGameWon;

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

                // Empire-specific SignalR events
                HubService.OnActionTaken += HandleActionTaken;
                HubService.OnInitiativePassed += HandleInitiativePassed;
                HubService.OnPlayerPassed += HandlePlayerPassed;
                HubService.OnPhaseTransition += HandlePhaseTransition;
                HubService.OnCardExertionToggled += HandleCardExertionToggled;
                HubService.OnCardMoved += HandleCardMoved;
                HubService.OnDamageAssigned += HandleDamageAssigned;
                HubService.OnMoraleUpdated += HandleMoraleUpdated;
                HubService.OnAllCardsUnexerted += HandleAllCardsUnexerted;

                isLoading = false;
                ChatLog.Add(("System", "🏛️ Empire game initialized!"));
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to initialize Empire game: {ex.Message}";
                isLoading = false;
            }
        }

        // Empire Event Handlers
        private async Task HandleEmpirePhaseChanged(GamePhase phase, string initiativeHolder)
        {
            ChatLog.Add(("System", $"⏰ Phase: {phase} | Initiative: {initiativeHolder}"));
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleInitiativeChanged(string newInitiativeHolder)
        {
            var isYourTurn = newInitiativeHolder == playerId;
            ChatLog.Add(("System", isYourTurn ? "🎯 Your turn!" : "⏳ Opponent's turn"));
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleMoraleChanged(string playerId, int newMorale)
        {
            ChatLog.Add(("System", $"💔 {playerId} morale: {newMorale}"));
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleGameWon(string winnerId)
        {
            var isYouWinner = winnerId == playerId;
            ChatLog.Add(("System", isYouWinner ? "🎉 You won!" : "💀 You lost!"));
            await InvokeAsync(StateHasChanged);
        }

        // SignalR Event Handlers
        private async Task HandleActionTaken(string playerId, string actionType, object actionData)
        {
            ChatLog.Add((playerId, $"🎯 {actionType}"));
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleInitiativePassed(string playerId)
        {
            ChatLog.Add((playerId, "⏭️ Passed initiative"));
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandlePlayerPassed(string playerId)
        {
            ChatLog.Add((playerId, "⏸️ Passed"));
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandlePhaseTransition(string newPhase, string initiativeHolder)
        {
            ChatLog.Add(("System", $"🔄 Phase: {newPhase} | Initiative: {initiativeHolder}"));
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleCardExertionToggled(string playerId, int cardId, bool isExerted)
        {
            var card = allCards.FirstOrDefault(c => c.CardID == cardId);
            var action = isExerted ? "exerted" : "unexerted";
            ChatLog.Add((playerId, $"⚡ {action} {card?.Name ?? $"Card #{cardId}"}"));
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleCardMoved(string playerId, int cardId, string fromZone, string toZone)
        {
            var card = allCards.FirstOrDefault(c => c.CardID == cardId);
            ChatLog.Add((playerId, $"🔄 Moved {card?.Name ?? $"Card #{cardId}"} from {fromZone} to {toZone}"));
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleDamageAssigned(string playerId, string territoryId, object damageAssignment)
        {
            ChatLog.Add((playerId, $"⚔️ Assigned damage in {territoryId}"));
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleMoraleUpdated(string playerId, int newMorale, int damage)
        {
            ChatLog.Add(("System", $"💔 {playerId} took {damage} damage (Morale: {newMorale})"));
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleAllCardsUnexerted(string playerId)
        {
            ChatLog.Add((playerId, "🔄 Unexerted all cards"));
            await InvokeAsync(StateHasChanged);
        }

        // Legacy SignalR handlers (keeping for compatibility)
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

        // Empire Game Actions
        private async Task DeployArmyCard(int cardId)
        {
            try
            {
                bool success = await EmpireGameService.DeployArmyCard(cardId);
                if (success)
                {
                    var card = allCards.FirstOrDefault(c => c.CardID == cardId);
                    ChatLog.Add((playerId, $"⚔️ Deployed {card?.Name ?? $"Card #{cardId}"}"));
                }
                else
                {
                    ChatLog.Add(("System", "❌ Cannot deploy army card"));
                }
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                ChatLog.Add(("System", $"❌ Error deploying army card: {ex.Message}"));
            }
        }

        private async Task PlayVillager(int cardId)
        {
            try
            {
                bool success = await EmpireGameService.PlayVillager(cardId);
                if (success)
                {
                    var card = allCards.FirstOrDefault(c => c.CardID == cardId);
                    ChatLog.Add((playerId, $"👥 Played villager {card?.Name ?? $"Card #{cardId}"}"));
                }
                else
                {
                    ChatLog.Add(("System", "❌ Cannot play villager"));
                }
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                ChatLog.Add(("System", $"❌ Error playing villager: {ex.Message}"));
            }
        }

        private async Task PassInitiative()
        {
            try
            {
                await EmpireGameService.PassInitiative();
                ChatLog.Add((playerId, "⏭️ Passed initiative"));
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                ChatLog.Add(("System", $"❌ Error passing initiative: {ex.Message}"));
            }
        }

        // Card interaction handlers
        private async Task HandleCardClick(int cardId)
        {
            var card = allCards.FirstOrDefault(c => c.CardID == cardId);
            Console.WriteLine($"Clicked: {card?.Name ?? "Unknown"} (#{cardId})");
            
            // Show card details or select for action
            await Task.CompletedTask;
        }

        private async Task HandleCardDoubleClick(int cardId)
        {
            var card = allCards.FirstOrDefault(c => c.CardID == cardId);
            Console.WriteLine($"Double-clicked: {card?.Name ?? "Unknown"} (#{cardId})");
            
            // Toggle exertion for board cards
            if (PlayerBoard.Any(bc => bc.CardId == cardId) && IsMyTurn)
            {
                await EmpireGameService.ToggleCardExertion(cardId);
            }
        }

        private async Task HandleHandCardClick(int cardId)
        {
            var card = allCards.FirstOrDefault(c => c.CardID == cardId);
            Console.WriteLine($"Hand card clicked: {card?.Name ?? "Unknown"} (#{cardId})");
            await Task.CompletedTask;
        }

        private async Task HandleHandCardDoubleClick(int cardId)
        {
            if (!IsMyTurn) return;

            var card = allCards.FirstOrDefault(c => c.CardID == cardId);
            
            // Determine action based on card type
            if (card?.CardType == "Villager")
            {
                await PlayVillager(cardId);
            }
            else if (IsArmyCard(card))
            {
                await DeployArmyCard(cardId);
            }
            else
            {
                ChatLog.Add(("System", $"❓ Unknown action for {card?.CardType ?? "Unknown"} card"));
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

        // Drag and drop
        private void OnDragStart(int cardId) => draggedCardId = cardId;

        private async Task OnCardDrop()
        {
            if (!draggedCardId.HasValue || !IsMyTurn) return;

            try
            {
                var card = allCards.FirstOrDefault(c => c.CardID == draggedCardId.Value);
                
                // Handle different card types
                if (card?.CardType == "Villager")
                {
                    await PlayVillager(draggedCardId.Value);
                }
                else if (IsArmyCard(card))
                {
                    await DeployArmyCard(draggedCardId.Value);
                }
                else
                {
                    ChatLog.Add(("System", $"❓ Cannot play {card?.CardType ?? "Unknown"} card"));
                }
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

        // Drawing cards (Empire style)
        private async Task DrawCivic()
        {
            try
            {
                bool success = await GameApi.DrawCards(gameId, playerId, false); // false = civic
                if (success)
                {
                    ChatLog.Add((playerId, "🃏 Drew 2 civic cards"));
                }
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                ChatLog.Add(("System", $"❌ Error drawing civic cards: {ex.Message}"));
            }
        }

        private async Task DrawMilitary()
        {
            try
            {
                bool success = await GameApi.DrawCards(gameId, playerId, true); // true = army
                if (success)
                {
                    ChatLog.Add((playerId, "🃏 Drew 1 army card"));
                }
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                ChatLog.Add(("System", $"❌ Error drawing army card: {ex.Message}"));
            }
        }

        // Chat and commands
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
                case "/pass":
                    await PassInitiative();
                    break;
                case "/draw":
                    await DrawMilitary();
                    break;
                case "/drawcivic":
                    await DrawCivic();
                    break;
                case "/unexert":
                    await EmpireGameService.UnexertAllCards();
                    ChatLog.Add((playerId, "🔄 Unexerted all cards"));
                    break;
                default:
                    ChatLog.Add((playerId, $"❓ Unknown command: {command}"));
                    break;
            }
            await InvokeAsync(StateHasChanged);
        }

        // UI helpers
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

        private bool IsArmyCard(CardData? card)
        {
            return card?.CardType is "Unit" or "Tactic" or "Battle Tactic" or "Chronicle" or "Skirmisher";
        }

        private string GetOpponentId()
        {
            return gameState?.Player1 == playerId ? gameState?.Player2 ?? "" : gameState?.Player1 ?? "";
        }

        private List<BoardCard> GetOpponentBoard()
        {
            var opponentId = GetOpponentId();
            if (string.IsNullOrEmpty(opponentId)) return new();
            
            var heartland = gameState?.PlayerHeartlands.GetValueOrDefault(opponentId, new()) ?? new();
            var villagers = gameState?.PlayerVillagers.GetValueOrDefault(opponentId, new()) ?? new();
            
            return heartland.Concat(villagers).Select(cardId => new BoardCard(cardId)).ToList();
        }

        private async Task RefreshGameState()
        {
            await EmpireGameService.RefreshGameState();
            await InvokeAsync(StateHasChanged);
        }

        private void NavigateToLobby()
        {
            NavigationManager.NavigateTo("/lobby");
        }

        private async Task EndTurn()
        {
            await PassInitiative();
        }

        private async Task ShuffleDeck()
        {
            ChatLog.Add((playerId, "🔀 Shuffled deck"));
            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            // Unsubscribe from Empire events
            if (EmpireGameService != null)
            {
                EmpireGameService.OnPhaseChanged -= HandleEmpirePhaseChanged;
                EmpireGameService.OnInitiativeChanged -= HandleInitiativeChanged;
                EmpireGameService.OnMoraleChanged -= HandleMoraleChanged;
                EmpireGameService.OnGameWon -= HandleGameWon;
            }

            // Unsubscribe from SignalR events
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
                
                // Empire-specific events
                HubService.OnActionTaken -= HandleActionTaken;
                HubService.OnInitiativePassed -= HandleInitiativePassed;
                HubService.OnPlayerPassed -= HandlePlayerPassed;
                HubService.OnPhaseTransition -= HandlePhaseTransition;
                HubService.OnCardExertionToggled -= HandleCardExertionToggled;
                HubService.OnCardMoved -= HandleCardMoved;
                HubService.OnDamageAssigned -= HandleDamageAssigned;
                HubService.OnMoraleUpdated -= HandleMoraleUpdated;
                HubService.OnAllCardsUnexerted -= HandleAllCardsUnexerted;
            }
        }
    }
}
