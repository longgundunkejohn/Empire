using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Empire.Shared.Models.DTOs;

namespace Empire.Client.Services;

public class GameHubService : IAsyncDisposable
{
    private readonly NavigationManager _nav;
    private HubConnection? _hub;

    public event Func<BoardPositionUpdate, Task>? OnBoardUpdate;

    public GameHubService(NavigationManager nav)
    {
        _nav = nav;
    }

    public async Task ConnectAsync(string gameId)
    {
        if (_hub != null && _hub.State == HubConnectionState.Connected)
            return;

        _hub = new HubConnectionBuilder()
            .WithUrl(_nav.ToAbsoluteUri("/gamehub"))
            .WithAutomaticReconnect()
            .Build();

        _hub.On<BoardPositionUpdate>("ReceiveBoardUpdate", async (update) =>
        {
            if (OnBoardUpdate != null)
                await OnBoardUpdate.Invoke(update);
        });

        _hub.Closed += async (error) =>
        {
            Console.WriteLine("❌ Hub connection closed. Reconnecting...");
            await Task.Delay(2000);
            await ConnectAsync(gameId);
        };

        await _hub.StartAsync();
        await _hub.SendAsync("JoinGameGroup", gameId);

        Console.WriteLine($"✅ Hub connected and joined game group: {gameId}");
    }

    public async Task SendBoardUpdate(string gameId, BoardPositionUpdate update)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("SendBoardUpdate", update);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot send update.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
        {
            await _hub.DisposeAsync();
        }
    }
}
