using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Empire.Server.Hubs
{
    public class GameHub : Hub
    {
        public async Task JoinGameGroup(string gameId)
            => await Groups.AddToGroupAsync(Context.ConnectionId, gameId);

        public async Task SendBoardUpdate(BoardPositionUpdate update)
            => await Clients.OthersInGroup(update.GameId)
                .SendAsync("ReceiveBoardUpdate", update);
    }
}
