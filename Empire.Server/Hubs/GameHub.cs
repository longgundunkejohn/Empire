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
    }
}
