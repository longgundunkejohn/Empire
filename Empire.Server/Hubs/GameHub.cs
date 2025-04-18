using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Empire.Shared.Models.DTOs;

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
    }
}
