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
