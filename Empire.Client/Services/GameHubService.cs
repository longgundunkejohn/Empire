using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Empire.Shared.Models.DTOs;
using Empire.Shared.Models;

namespace Empire.Client.Services;

public class GameHubService : IAsyncDisposable
{
    private readonly NavigationManager _nav;
    private HubConnection? _hub;

    public event Func<BoardPositionUpdate, Task>? OnBoardUpdate;
    public event Func<string, Task>? OnGameStateUpdated;
    public event Func<string, string, Task>? OnChatMessage;
    public event Func<GameMove, Task>? OnMoveSubmitted;
    public event Func<string, string, Task>? OnPhaseChanged;
    public event Func<string, int, Task>? OnCardDrawn;
    public event Func<string, int, Task>? OnCardPlayed;
    public event Func<string, Task>? OnPlayerJoined;
    public event Func<string, Task>? OnGameStarted;

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

        // Register all event handlers
        _hub.On<BoardPositionUpdate>("ReceiveBoardUpdate", async (update) =>
        {
            if (OnBoardUpdate != null)
                await OnBoardUpdate.Invoke(update);
        });

        _hub.On<string>("GameStateUpdated", async (updatedGameId) =>
        {
            if (OnGameStateUpdated != null)
                await OnGameStateUpdated.Invoke(updatedGameId);
        });

        _hub.On<string, string>("ReceiveChatMessage", async (playerId, message) =>
        {
            if (OnChatMessage != null)
                await OnChatMessage.Invoke(playerId, message);
        });

        _hub.On<GameMove>("MoveSubmitted", async (move) =>
        {
            if (OnMoveSubmitted != null)
                await OnMoveSubmitted.Invoke(move);
        });

        _hub.On<string, string>("PhaseChanged", async (newPhase, activePlayer) =>
        {
            if (OnPhaseChanged != null)
                await OnPhaseChanged.Invoke(newPhase, activePlayer);
        });

        _hub.On<string, int>("CardDrawn", async (playerId, cardId) =>
        {
            if (OnCardDrawn != null)
                await OnCardDrawn.Invoke(playerId, cardId);
        });

        _hub.On<string, int>("CardPlayed", async (playerId, cardId) =>
        {
            if (OnCardPlayed != null)
                await OnCardPlayed.Invoke(playerId, cardId);
        });

        _hub.On<string>("PlayerJoined", async (playerId) =>
        {
            if (OnPlayerJoined != null)
                await OnPlayerJoined.Invoke(playerId);
        });

        _hub.On<string>("GameStarted", async (gameId) =>
        {
            if (OnGameStarted != null)
                await OnGameStarted.Invoke(gameId);
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
            Console.WriteLine("⚠️ Hub not connected. Cannot send board update.");
        }
    }

    public async Task NotifyGameStateUpdated(string gameId)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("NotifyGameStateUpdated", gameId);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot send game state update.");
        }
    }

    public async Task SendChatMessage(string gameId, string playerId, string message)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("SendChatMessage", gameId, playerId, message);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot send chat message.");
        }
    }

    public async Task NotifyMoveSubmitted(string gameId, GameMove move)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("NotifyMoveSubmitted", gameId, move);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot notify move submitted.");
        }
    }

    public async Task NotifyPhaseChange(string gameId, string newPhase, string activePlayer)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("NotifyPhaseChange", gameId, newPhase, activePlayer);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot notify phase change.");
        }
    }

    public async Task NotifyCardDrawn(string gameId, string playerId, int cardId)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("NotifyCardDrawn", gameId, playerId, cardId);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot notify card drawn.");
        }
    }

    public async Task NotifyCardPlayed(string gameId, string playerId, int cardId)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("NotifyCardPlayed", gameId, playerId, cardId);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot notify card played.");
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
