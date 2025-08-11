using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Empire.Shared.Models.DTOs;
using Empire.Shared.Models;

namespace Empire.Server.Hubs
{
    public class GameHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"🔌 Connected: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"❌ Disconnected: {Context.ConnectionId}");
            return base.OnDisconnectedAsync(exception);
        }

        public async Task JoinGameGroup(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId))
            {
                Console.WriteLine($"⚠️ Invalid gameId on JoinGameGroup: {gameId}");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            Console.WriteLine($"👥 {Context.ConnectionId} joined game {gameId}");
        }

        // Legacy methods (keeping for compatibility)
        public async Task SendBoardUpdate(BoardPositionUpdate update)
        {
            if (update == null || string.IsNullOrWhiteSpace(update.GameId))
            {
                Console.WriteLine("⚠️ Invalid or null board update received.");
                return;
            }

            await Clients.OthersInGroup(update.GameId)
                .SendAsync("ReceiveBoardUpdate", update);

            Console.WriteLine($"📤 Board update sent for game {update.GameId}");
        }

        public async Task NotifyGameStateUpdated(string gameId)
        {
            if (!string.IsNullOrEmpty(gameId))
            {
                await Clients.Group(gameId).SendAsync("GameStateUpdated", gameId);
                Console.WriteLine($"🔄 Notified GameStateUpdated for {gameId}");
            }
        }

        public async Task SendChatMessage(string gameId, string playerId, string message)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(message))
                return;

            await Clients.Group(gameId).SendAsync("ReceiveChatMessage", playerId, message);
            Console.WriteLine($"💬 Chat message sent in game {gameId} from {playerId}");
        }

        // Empire-specific methods
        
        /// <summary>
        /// Empire Initiative System: Player takes an action, initiative passes to opponent
        /// </summary>
        public async Task TakeAction(string gameId, string playerId, string actionType, object actionData)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            // Notify all players that an action was taken and initiative has passed
            await Clients.Group(gameId).SendAsync("ActionTaken", playerId, actionType, actionData);
            await Clients.Group(gameId).SendAsync("InitiativePassed", playerId);
            
            Console.WriteLine($"⚡ Action '{actionType}' taken by {playerId} in game {gameId}, initiative passed");
        }

        /// <summary>
        /// Empire Initiative System: Player passes their turn
        /// </summary>
        public async Task PassInitiative(string gameId, string playerId)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("PlayerPassed", playerId);
            Console.WriteLine($"⏭️ Player {playerId} passed initiative in game {gameId}");
        }

        /// <summary>
        /// Empire Phase System: Notify when both players have passed and phase should advance
        /// </summary>
        public async Task NotifyPhaseTransition(string gameId, GamePhase newPhase, string initiativeHolder)
        {
            if (string.IsNullOrWhiteSpace(gameId))
                return;

            await Clients.Group(gameId).SendAsync("PhaseTransition", newPhase.ToString(), initiativeHolder);
            Console.WriteLine($"🔄 Phase transition to {newPhase} in game {gameId}, initiative to {initiativeHolder}");
        }

        /// <summary>
        /// Empire Card Actions: Deploy army card
        /// </summary>
        public async Task DeployArmyCard(string gameId, string playerId, int cardId, int manaCost)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await TakeAction(gameId, playerId, "DeployArmyCard", new { cardId, manaCost });
        }

        /// <summary>
        /// Empire Card Actions: Play villager (once per round)
        /// </summary>
        public async Task PlayVillager(string gameId, string playerId, int cardId)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await TakeAction(gameId, playerId, "PlayVillager", new { cardId });
        }

        /// <summary>
        /// Empire Card Actions: Settle territory (once per round)
        /// </summary>
        public async Task SettleTerritory(string gameId, string playerId, int cardId, string territoryId)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await TakeAction(gameId, playerId, "SettleTerritory", new { cardId, territoryId });
        }

        /// <summary>
        /// Empire Card Actions: Commit units to territories (once per round)
        /// </summary>
        public async Task CommitUnits(string gameId, string playerId, object commitData)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await TakeAction(gameId, playerId, "CommitUnits", commitData);
        }

        /// <summary>
        /// Empire Card Actions: Exert/Unexert card (double-click)
        /// </summary>
        public async Task ToggleCardExertion(string gameId, string playerId, int cardId, bool isExerted)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("CardExertionToggled", playerId, cardId, isExerted);
            Console.WriteLine($"🔄 Card {cardId} exertion toggled to {isExerted} by {playerId} in game {gameId}");
        }

        /// <summary>
        /// Empire Card Actions: Move card between zones
        /// </summary>
        public async Task MoveCard(string gameId, string playerId, int cardId, string fromZone, string toZone)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("CardMoved", playerId, cardId, fromZone, toZone);
            Console.WriteLine($"📦 Card {cardId} moved from {fromZone} to {toZone} by {playerId} in game {gameId}");
        }

        /// <summary>
        /// Empire Combat: Assign damage in territory
        /// </summary>
        public async Task AssignDamage(string gameId, string playerId, string territoryId, object damageAssignment)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("DamageAssigned", playerId, territoryId, damageAssignment);
            Console.WriteLine($"⚔️ Damage assigned in {territoryId} by {playerId} in game {gameId}");
        }

        /// <summary>
        /// Empire Morale: Update player morale
        /// </summary>
        public async Task UpdateMorale(string gameId, string playerId, int newMorale, int damage)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("MoraleUpdated", playerId, newMorale, damage);
            Console.WriteLine($"💔 Morale updated for {playerId} to {newMorale} (-{damage}) in game {gameId}");
        }

        /// <summary>
        /// Empire Replenishment: Unexert all cards
        /// </summary>
        public async Task UnexertAllCards(string gameId, string playerId)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("AllCardsUnexerted", playerId);
            Console.WriteLine($"🔄 All cards unexerted for {playerId} in game {gameId}");
        }

        // Legacy notification methods (keeping for compatibility)
        public async Task NotifyMoveSubmitted(string gameId, GameMove move)
        {
            if (string.IsNullOrWhiteSpace(gameId) || move == null)
                return;

            await Clients.OthersInGroup(gameId).SendAsync("MoveSubmitted", move);
            Console.WriteLine($"🎯 Move {move.MoveType} submitted in game {gameId} by {move.PlayerId}");
        }

        public async Task NotifyPhaseChange(string gameId, string newPhase, string activePlayer)
        {
            if (string.IsNullOrWhiteSpace(gameId))
                return;

            await Clients.Group(gameId).SendAsync("PhaseChanged", newPhase, activePlayer);
            Console.WriteLine($"⏰ Phase changed to {newPhase} in game {gameId}, active player: {activePlayer}");
        }

        public async Task NotifyCardDrawn(string gameId, string playerId, int cardId)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.OthersInGroup(gameId).SendAsync("CardDrawn", playerId, cardId);
            Console.WriteLine($"🃏 Card {cardId} drawn by {playerId} in game {gameId}");
        }

        public async Task NotifyCardPlayed(string gameId, string playerId, int cardId)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.OthersInGroup(gameId).SendAsync("CardPlayed", playerId, cardId);
            Console.WriteLine($"🎴 Card {cardId} played by {playerId} in game {gameId}");
        }

        public async Task NotifyPlayerJoined(string gameId, string playerId)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("PlayerJoined", playerId);
            Console.WriteLine($"👋 Player {playerId} joined game {gameId}");
        }

        public async Task NotifyGameStarted(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId))
                return;

            await Clients.Group(gameId).SendAsync("GameStarted", gameId);
            Console.WriteLine($"🚀 Game {gameId} started");
        }

        // Game Room specific methods for lobby functionality

        /// <summary>
        /// Join a specific game room for lobby functionality
        /// </summary>
        public async Task JoinGameRoom(string lobbyId)
        {
            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                Console.WriteLine($"⚠️ Invalid lobbyId on JoinGameRoom: {lobbyId}");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"lobby_{lobbyId}");
            Console.WriteLine($"🏠 {Context.ConnectionId} joined lobby room {lobbyId}");
        }

        /// <summary>
        /// Leave a specific game room
        /// </summary>
        public async Task LeaveGameRoom(string lobbyId)
        {
            if (string.IsNullOrWhiteSpace(lobbyId))
                return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"lobby_{lobbyId}");
            Console.WriteLine($"🚪 {Context.ConnectionId} left lobby room {lobbyId}");
        }

        /// <summary>
        /// Notify when a player joins the lobby
        /// </summary>
        public async Task NotifyPlayerJoinedLobby(string lobbyId, string username)
        {
            if (string.IsNullOrWhiteSpace(lobbyId) || string.IsNullOrWhiteSpace(username))
                return;

            await Clients.Group($"lobby_{lobbyId}").SendAsync("PlayerJoined", lobbyId, username);
            Console.WriteLine($"👋 Player {username} joined lobby {lobbyId}");
        }

        /// <summary>
        /// Notify when a player leaves the lobby
        /// </summary>
        public async Task NotifyPlayerLeftLobby(string lobbyId, string username)
        {
            if (string.IsNullOrWhiteSpace(lobbyId) || string.IsNullOrWhiteSpace(username))
                return;

            await Clients.Group($"lobby_{lobbyId}").SendAsync("PlayerLeft", lobbyId, username);
            Console.WriteLine($"👋 Player {username} left lobby {lobbyId}");
        }

        /// <summary>
        /// Notify when a player's ready status changes
        /// </summary>
        public async Task NotifyPlayerReady(string lobbyId, bool ready)
        {
            if (string.IsNullOrWhiteSpace(lobbyId))
                return;

            // We'll need to get the player ID from the context or pass it as a parameter
            // For now, using a placeholder - this should be enhanced with proper user context
            var playerId = Context.UserIdentifier ?? Context.ConnectionId;
            
            await Clients.OthersInGroup($"lobby_{lobbyId}").SendAsync("PlayerReady", lobbyId, playerId, ready);
            Console.WriteLine($"✅ Player {playerId} ready status: {ready} in lobby {lobbyId}");
        }

        /// <summary>
        /// Notify when the game starts from lobby
        /// </summary>
        public async Task NotifyLobbyGameStarted(string lobbyId)
        {
            if (string.IsNullOrWhiteSpace(lobbyId))
                return;

            await Clients.Group($"lobby_{lobbyId}").SendAsync("GameStarted", lobbyId);
            Console.WriteLine($"🚀 Game started from lobby {lobbyId}");
        }

        /// <summary>
        /// Start a game from lobby - integrates with LobbyService
        /// </summary>
        public async Task StartGameFromLobby(string lobbyId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    await Clients.Caller.SendAsync("Error", "Authentication required");
                    return;
                }

                // The actual game start logic is handled by the LobbyController
                // This method is for SignalR coordination
                await Clients.Group($"lobby_{lobbyId}").SendAsync("GameStarting", lobbyId);
                Console.WriteLine($"🎮 Game starting initiated for lobby {lobbyId} by user {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error starting game from lobby {lobbyId}: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "Failed to start game");
            }
        }

        private int GetCurrentUserId()
        {
            // Extract user ID from claims or connection context
            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = Context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(userIdClaim, out var userId) ? userId : 0;
            }
            return 0;
        }

        // ========================================
        // MANUAL PLAY METHODS (Cockatrice-style)
        // ========================================

        /// <summary>
        /// Manual Play: Pass action priority to opponent
        /// </summary>
        public async Task PassActionPriority(string gameId, string playerId)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("ActionPriorityPassed", playerId);
            Console.WriteLine($"⏭️ Action priority passed by {playerId} in game {gameId}");
        }

        /// <summary>
        /// Manual Play: Pass initiative (end of round)
        /// </summary>
        public async Task PassInitiativeManual(string gameId, string playerId)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("InitiativePassed", playerId);
            Console.WriteLine($"🔄 Initiative passed by {playerId} in game {gameId}");
        }

        /// <summary>
        /// Manual Play: Move card between zones
        /// </summary>
        public async Task MoveCardManual(string gameId, string playerId, int cardId, string fromZone, string toZone, int? position = null)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("CardMovedManual", playerId, cardId, fromZone, toZone, position);
            await PassActionPriority(gameId, playerId);
            Console.WriteLine($"📦 Manual: Card {cardId} moved from {fromZone} to {toZone} by {playerId} in game {gameId}");
        }

        /// <summary>
        /// Manual Play: Move multiple cards between zones
        /// </summary>
        public async Task MoveMultipleCardsManual(string gameId, string playerId, List<int> cardIds, string fromZone, string toZone)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId) || cardIds == null || !cardIds.Any())
                return;

            await Clients.Group(gameId).SendAsync("MultipleCardsMovedManual", playerId, cardIds, fromZone, toZone);
            await PassActionPriority(gameId, playerId);
            Console.WriteLine($"📦 Manual: {cardIds.Count} cards moved from {fromZone} to {toZone} by {playerId} in game {gameId}");
        }

        /// <summary>
        /// Manual Play: Toggle card tapped/untapped state
        /// </summary>
        public async Task ToggleCardTappedManual(string gameId, string playerId, int cardId, bool isTapped)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("CardTappedToggled", playerId, cardId, isTapped);
            Console.WriteLine($"🔄 Manual: Card {cardId} {(isTapped ? "tapped" : "untapped")} by {playerId} in game {gameId}");
        }

        /// <summary>
        /// Manual Play: Flip card face up/down
        /// </summary>
        public async Task FlipCardManual(string gameId, string playerId, int cardId, bool faceUp)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("CardFlipped", playerId, cardId, faceUp);
            Console.WriteLine($"🔄 Manual: Card {cardId} flipped {(faceUp ? "face up" : "face down")} by {playerId} in game {gameId}");
        }

        /// <summary>
        /// Manual Play: Add counters to card
        /// </summary>
        public async Task AddCounterManual(string gameId, string playerId, int cardId, string counterType, int amount)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("CounterAdded", playerId, cardId, counterType, amount);
            Console.WriteLine($"➕ Manual: Added {amount} {counterType} counter(s) to card {cardId} by {playerId} in game {gameId}");
        }

        /// <summary>
        /// Manual Play: Remove counters from card
        /// </summary>
        public async Task RemoveCounterManual(string gameId, string playerId, int cardId, string counterType, int amount)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("CounterRemoved", playerId, cardId, counterType, amount);
            Console.WriteLine($"➖ Manual: Removed {amount} {counterType} counter(s) from card {cardId} by {playerId} in game {gameId}");
        }

        /// <summary>
        /// Manual Play: Adjust player morale
        /// </summary>
        public async Task AdjustMoraleManual(string gameId, string playerId, int amount)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("MoraleAdjusted", playerId, amount);
            Console.WriteLine($"💔 Manual: Morale adjusted by {amount} for {playerId} in game {gameId}");
        }

        /// <summary>
        /// Manual Play: Set player tier
        /// </summary>
        public async Task SetTierManual(string gameId, string playerId, int newTier)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("TierSet", playerId, newTier);
            Console.WriteLine($"🏆 Manual: Tier set to {newTier} for {playerId} in game {gameId}");
        }

        /// <summary>
        /// Manual Play: Draw cards from deck
        /// </summary>
        public async Task DrawCardsManual(string gameId, string playerId, string deckType, int count)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("CardsDrawn", playerId, deckType, count);
            await PassActionPriority(gameId, playerId);
            Console.WriteLine($"🃏 Manual: Drew {count} {deckType} card(s) for {playerId} in game {gameId}");
        }

        /// <summary>
        /// Manual Play: Shuffle deck
        /// </summary>
        public async Task ShuffleDeckManual(string gameId, string playerId, string deckType)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("DeckShuffled", playerId, deckType);
            Console.WriteLine($"🔀 Manual: {deckType} deck shuffled for {playerId} in game {gameId}");
        }

        /// <summary>
        /// Manual Play: Untap all units (batch operation)
        /// </summary>
        public async Task UntapAllUnitsManual(string gameId, string playerId)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("AllUnitsUntapped", playerId);
            Console.WriteLine($"🔄 Manual: All units untapped for {playerId} in game {gameId}");
        }

        /// <summary>
        /// Manual Play: Advance game phase
        /// </summary>
        public async Task AdvancePhaseManual(string gameId, string playerId, GamePhase newPhase)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("PhaseAdvanced", playerId, newPhase.ToString());
            Console.WriteLine($"⏰ Manual: Phase advanced to {newPhase} by {playerId} in game {gameId}");
        }

        /// <summary>
        /// Manual Play: Advance round
        /// </summary>
        public async Task AdvanceRoundManual(string gameId, string playerId, int newRound)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("RoundAdvanced", playerId, newRound);
            Console.WriteLine($"🔄 Manual: Round advanced to {newRound} by {playerId} in game {gameId}");
        }

        /// <summary>
        /// Manual Play: Ping system for highlighting
        /// </summary>
        public async Task PingManual(string gameId, string playerId, string targetType, string targetId, string message)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
                return;

            await Clients.Group(gameId).SendAsync("Ping", playerId, targetType, targetId, message);
            Console.WriteLine($"📍 Manual: Ping from {playerId} on {targetType} {targetId}: {message} in game {gameId}");
        }

        /// <summary>
        /// Manual Play: Chat message
        /// </summary>
        public async Task SendChatMessageManual(string gameId, string playerId, string message)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(message))
                return;

            await Clients.Group(gameId).SendAsync("ChatMessage", playerId, message, DateTime.UtcNow);
            Console.WriteLine($"💬 Manual: Chat message from {playerId} in game {gameId}: {message}");
        }

        /// <summary>
        /// Manual Play: Start game from lobby (initialize manual game state)
        /// </summary>
        public async Task StartManualGame(string gameId, string player1Id, string player2Id)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(player1Id) || string.IsNullOrWhiteSpace(player2Id))
                return;

            await Clients.Group(gameId).SendAsync("ManualGameStarted", player1Id, player2Id);
            Console.WriteLine($"🚀 Manual: Game started between {player1Id} and {player2Id} in game {gameId}");
        }

        /// <summary>
        /// Manual Play: Update full game state (for synchronization)
        /// </summary>
        public async Task UpdateGameStateManual(string gameId, object gameState)
        {
            if (string.IsNullOrWhiteSpace(gameId) || gameState == null)
                return;

            await Clients.Group(gameId).SendAsync("GameStateUpdatedManual", gameState);
            Console.WriteLine($"🔄 Manual: Game state updated for game {gameId}");
        }
    }
}
