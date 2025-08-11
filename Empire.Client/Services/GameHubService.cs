using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Empire.Shared.Models.DTOs;
using Empire.Shared.Models;

namespace Empire.Client.Services;

public class GameHubService : IAsyncDisposable
{
    private readonly NavigationManager _nav;
    private HubConnection? _hub;

    // Legacy events (keeping for compatibility)
    public event Func<BoardPositionUpdate, Task>? OnBoardUpdate;
    public event Func<string, Task>? OnGameStateUpdated;
    public event Func<string, string, Task>? OnChatMessage;
    public event Func<GameMove, Task>? OnMoveSubmitted;
    public event Func<string, string, Task>? OnPhaseChanged;
    public event Func<string, int, Task>? OnCardDrawn;
    public event Func<string, int, Task>? OnCardPlayed;
    public event Func<string, Task>? OnPlayerJoined;
    public event Func<string, Task>? OnGameStarted;

    // Empire-specific events
    public event Func<string, string, object, Task>? OnActionTaken;
    public event Func<string, Task>? OnInitiativePassed;
    public event Func<string, Task>? OnPlayerPassed;
    public event Func<string, string, Task>? OnPhaseTransition;
    public event Func<string, int, bool, Task>? OnCardExertionToggled;
    public event Func<string, int, string, string, Task>? OnCardMoved;
    public event Func<string, string, object, Task>? OnDamageAssigned;
    public event Func<string, int, int, Task>? OnMoraleUpdated;
    public event Func<string, Task>? OnAllCardsUnexerted;

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

        // Empire-specific event handlers
        _hub.On<string, string, object>("ActionTaken", async (playerId, actionType, actionData) =>
        {
            if (OnActionTaken != null)
                await OnActionTaken.Invoke(playerId, actionType, actionData);
        });

        _hub.On<string>("InitiativePassed", async (playerId) =>
        {
            if (OnInitiativePassed != null)
                await OnInitiativePassed.Invoke(playerId);
        });

        _hub.On<string>("PlayerPassed", async (playerId) =>
        {
            if (OnPlayerPassed != null)
                await OnPlayerPassed.Invoke(playerId);
        });

        _hub.On<string, string>("PhaseTransition", async (newPhase, initiativeHolder) =>
        {
            if (OnPhaseTransition != null)
                await OnPhaseTransition.Invoke(newPhase, initiativeHolder);
        });

        _hub.On<string, int, bool>("CardExertionToggled", async (playerId, cardId, isExerted) =>
        {
            if (OnCardExertionToggled != null)
                await OnCardExertionToggled.Invoke(playerId, cardId, isExerted);
        });

        _hub.On<string, int, string, string>("CardMoved", async (playerId, cardId, fromZone, toZone) =>
        {
            if (OnCardMoved != null)
                await OnCardMoved.Invoke(playerId, cardId, fromZone, toZone);
        });

        _hub.On<string, string, object>("DamageAssigned", async (playerId, territoryId, damageAssignment) =>
        {
            if (OnDamageAssigned != null)
                await OnDamageAssigned.Invoke(playerId, territoryId, damageAssignment);
        });

        _hub.On<string, int, int>("MoraleUpdated", async (playerId, newMorale, damage) =>
        {
            if (OnMoraleUpdated != null)
                await OnMoraleUpdated.Invoke(playerId, newMorale, damage);
        });

        _hub.On<string>("AllCardsUnexerted", async (playerId) =>
        {
            if (OnAllCardsUnexerted != null)
                await OnAllCardsUnexerted.Invoke(playerId);
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

    // Empire-specific client methods
    
    /// <summary>
    /// Empire Initiative: Pass initiative to opponent
    /// </summary>
    public async Task PassInitiative(string gameId, string playerId)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("PassInitiative", gameId, playerId);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot pass initiative.");
        }
    }

    /// <summary>
    /// Empire Actions: Deploy army card
    /// </summary>
    public async Task DeployArmyCard(string gameId, string playerId, int cardId, int manaCost)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("DeployArmyCard", gameId, playerId, cardId, manaCost);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot deploy army card.");
        }
    }

    /// <summary>
    /// Empire Actions: Play villager (once per round)
    /// </summary>
    public async Task PlayVillager(string gameId, string playerId, int cardId)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("PlayVillager", gameId, playerId, cardId);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot play villager.");
        }
    }

    /// <summary>
    /// Empire Actions: Settle territory (once per round)
    /// </summary>
    public async Task SettleTerritory(string gameId, string playerId, int cardId, string territoryId)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("SettleTerritory", gameId, playerId, cardId, territoryId);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot settle territory.");
        }
    }

    /// <summary>
    /// Empire Actions: Commit units to territories (once per round)
    /// </summary>
    public async Task CommitUnits(string gameId, string playerId, object commitData)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("CommitUnits", gameId, playerId, commitData);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot commit units.");
        }
    }

    /// <summary>
    /// Empire Actions: Toggle card exertion (double-click)
    /// </summary>
    public async Task ToggleCardExertion(string gameId, string playerId, int cardId, bool isExerted)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("ToggleCardExertion", gameId, playerId, cardId, isExerted);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot toggle card exertion.");
        }
    }

    /// <summary>
    /// Empire Actions: Move card between zones
    /// </summary>
    public async Task MoveCard(string gameId, string playerId, int cardId, string fromZone, string toZone)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("MoveCard", gameId, playerId, cardId, fromZone, toZone);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot move card.");
        }
    }

    /// <summary>
    /// Empire Combat: Assign damage in territory
    /// </summary>
    public async Task AssignDamage(string gameId, string playerId, string territoryId, object damageAssignment)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("AssignDamage", gameId, playerId, territoryId, damageAssignment);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot assign damage.");
        }
    }

    /// <summary>
    /// Empire Replenishment: Unexert all cards
    /// </summary>
    public async Task UnexertAllCards(string gameId, string playerId)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("UnexertAllCards", gameId, playerId);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot unexert all cards.");
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
