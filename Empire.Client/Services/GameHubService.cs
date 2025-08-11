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

    // Game Room events
    public event Func<string, string, Task>? OnPlayerJoined;
    public event Func<string, string, Task>? OnPlayerLeft;
    public event Func<string, int, bool, Task>? OnPlayerReady;
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

    public async Task ConnectAsync(string gameId = "")
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

        // Game Room event handlers
        _hub.On<string, string>("PlayerJoined", async (lobbyId, username) =>
        {
            if (OnPlayerJoined != null)
                await OnPlayerJoined.Invoke(lobbyId, username);
        });

        _hub.On<string, string>("PlayerLeft", async (lobbyId, username) =>
        {
            if (OnPlayerLeft != null)
                await OnPlayerLeft.Invoke(lobbyId, username);
        });

        _hub.On<string, int, bool>("PlayerReady", async (lobbyId, playerId, ready) =>
        {
            if (OnPlayerReady != null)
                await OnPlayerReady.Invoke(lobbyId, playerId, ready);
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

        // Register manual play event handlers
        RegisterManualPlayEventHandlers();

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

    // Game Room specific methods
    
    /// <summary>
    /// Join a specific game room for lobby functionality
    /// </summary>
    public async Task JoinGameRoomAsync(string lobbyId)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("JoinGameRoom", lobbyId);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot join game room.");
        }
    }

    /// <summary>
    /// Leave a specific game room
    /// </summary>
    public async Task LeaveGameRoomAsync(string lobbyId)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("LeaveGameRoom", lobbyId);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot leave game room.");
        }
    }

    /// <summary>
    /// Notify other players in the room about ready status change
    /// </summary>
    public async Task NotifyPlayerReadyAsync(string lobbyId, bool ready)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("NotifyPlayerReady", lobbyId, ready);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot notify player ready.");
        }
    }

    // ========================================
    // MANUAL PLAY METHODS (Cockatrice-style)
    // ========================================

    // Manual play events
    public event Func<string, Task>? OnActionPriorityPassed;
    public event Func<string, int, string, string, int?, Task>? OnCardMovedManual;
    public event Func<string, List<int>, string, string, Task>? OnMultipleCardsMovedManual;
    public event Func<string, int, bool, Task>? OnCardTappedToggled;
    public event Func<string, int, bool, Task>? OnCardFlipped;
    public event Func<string, int, string, int, Task>? OnCounterAdded;
    public event Func<string, int, string, int, Task>? OnCounterRemoved;
    public event Func<string, int, Task>? OnMoraleAdjustedManual;
    public event Func<string, int, Task>? OnTierSet;
    public event Func<string, string, int, Task>? OnCardsDrawn;
    public event Func<string, string, Task>? OnDeckShuffled;
    public event Func<string, Task>? OnAllUnitsUntapped;
    public event Func<string, string, Task>? OnPhaseAdvanced;
    public event Func<string, int, Task>? OnRoundAdvanced;
    public event Func<string, string, string, string, Task>? OnPing;
    public event Func<string, string, DateTime, Task>? OnChatMessageManual;
    public event Func<string, string, Task>? OnManualGameStarted;
    public event Func<object, Task>? OnGameStateUpdatedManual;

    /// <summary>
    /// Manual Play: Pass action priority to opponent
    /// </summary>
    public async Task PassActionPriorityManual(string gameId, string playerId)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("PassActionPriority", gameId, playerId);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot pass action priority.");
        }
    }

    /// <summary>
    /// Manual Play: Pass initiative (end of round)
    /// </summary>
    public async Task PassInitiativeManual(string gameId, string playerId)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("PassInitiativeManual", gameId, playerId);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot pass initiative.");
        }
    }

    /// <summary>
    /// Manual Play: Move card between zones
    /// </summary>
    public async Task MoveCardManual(string gameId, string playerId, int cardId, string fromZone, string toZone, int? position = null)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("MoveCardManual", gameId, playerId, cardId, fromZone, toZone, position);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot move card manually.");
        }
    }

    /// <summary>
    /// Manual Play: Move multiple cards between zones
    /// </summary>
    public async Task MoveMultipleCardsManual(string gameId, string playerId, List<int> cardIds, string fromZone, string toZone)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("MoveMultipleCardsManual", gameId, playerId, cardIds, fromZone, toZone);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot move multiple cards manually.");
        }
    }

    /// <summary>
    /// Manual Play: Toggle card tapped/untapped state
    /// </summary>
    public async Task ToggleCardTappedManual(string gameId, string playerId, int cardId, bool isTapped)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("ToggleCardTappedManual", gameId, playerId, cardId, isTapped);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot toggle card tapped state.");
        }
    }

    /// <summary>
    /// Manual Play: Flip card face up/down
    /// </summary>
    public async Task FlipCardManual(string gameId, string playerId, int cardId, bool faceUp)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("FlipCardManual", gameId, playerId, cardId, faceUp);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot flip card.");
        }
    }

    /// <summary>
    /// Manual Play: Add counters to card
    /// </summary>
    public async Task AddCounterManual(string gameId, string playerId, int cardId, string counterType, int amount)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("AddCounterManual", gameId, playerId, cardId, counterType, amount);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot add counter.");
        }
    }

    /// <summary>
    /// Manual Play: Remove counters from card
    /// </summary>
    public async Task RemoveCounterManual(string gameId, string playerId, int cardId, string counterType, int amount)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("RemoveCounterManual", gameId, playerId, cardId, counterType, amount);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot remove counter.");
        }
    }

    /// <summary>
    /// Manual Play: Adjust player morale
    /// </summary>
    public async Task AdjustMoraleManual(string gameId, string playerId, int amount)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("AdjustMoraleManual", gameId, playerId, amount);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot adjust morale.");
        }
    }

    /// <summary>
    /// Manual Play: Set player tier
    /// </summary>
    public async Task SetTierManual(string gameId, string playerId, int newTier)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("SetTierManual", gameId, playerId, newTier);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot set tier.");
        }
    }

    /// <summary>
    /// Manual Play: Draw cards from deck
    /// </summary>
    public async Task DrawCardsManual(string gameId, string playerId, string deckType, int count)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("DrawCardsManual", gameId, playerId, deckType, count);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot draw cards.");
        }
    }

    /// <summary>
    /// Manual Play: Shuffle deck
    /// </summary>
    public async Task ShuffleDeckManual(string gameId, string playerId, string deckType)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("ShuffleDeckManual", gameId, playerId, deckType);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot shuffle deck.");
        }
    }

    /// <summary>
    /// Manual Play: Untap all units (batch operation)
    /// </summary>
    public async Task UntapAllUnitsManual(string gameId, string playerId)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("UntapAllUnitsManual", gameId, playerId);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot untap all units.");
        }
    }

    /// <summary>
    /// Manual Play: Advance game phase
    /// </summary>
    public async Task AdvancePhaseManual(string gameId, string playerId, GamePhase newPhase)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("AdvancePhaseManual", gameId, playerId, newPhase);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot advance phase.");
        }
    }

    /// <summary>
    /// Manual Play: Advance round
    /// </summary>
    public async Task AdvanceRoundManual(string gameId, string playerId, int newRound)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("AdvanceRoundManual", gameId, playerId, newRound);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot advance round.");
        }
    }

    /// <summary>
    /// Manual Play: Ping system for highlighting
    /// </summary>
    public async Task PingManual(string gameId, string playerId, string targetType, string targetId, string message)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("PingManual", gameId, playerId, targetType, targetId, message);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot send ping.");
        }
    }

    /// <summary>
    /// Manual Play: Chat message
    /// </summary>
    public async Task SendChatMessageManual(string gameId, string playerId, string message)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("SendChatMessageManual", gameId, playerId, message);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot send chat message.");
        }
    }

    /// <summary>
    /// Manual Play: Start game from lobby (initialize manual game state)
    /// </summary>
    public async Task StartManualGame(string gameId, string player1Id, string player2Id)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("StartManualGame", gameId, player1Id, player2Id);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot start manual game.");
        }
    }

    /// <summary>
    /// Manual Play: Update full game state (for synchronization)
    /// </summary>
    public async Task UpdateGameStateManual(string gameId, object gameState)
    {
        if (_hub?.State == HubConnectionState.Connected)
        {
            await _hub.SendAsync("UpdateGameStateManual", gameId, gameState);
        }
        else
        {
            Console.WriteLine("⚠️ Hub not connected. Cannot update game state.");
        }
    }

    /// <summary>
    /// Register manual play event handlers
    /// </summary>
    private void RegisterManualPlayEventHandlers()
    {
        if (_hub == null) return;

        // Manual play event handlers
        _hub.On<string>("ActionPriorityPassed", async (playerId) =>
        {
            if (OnActionPriorityPassed != null)
                await OnActionPriorityPassed.Invoke(playerId);
        });

        _hub.On<string, int, string, string, int?>("CardMovedManual", async (playerId, cardId, fromZone, toZone, position) =>
        {
            if (OnCardMovedManual != null)
                await OnCardMovedManual.Invoke(playerId, cardId, fromZone, toZone, position);
        });

        _hub.On<string, List<int>, string, string>("MultipleCardsMovedManual", async (playerId, cardIds, fromZone, toZone) =>
        {
            if (OnMultipleCardsMovedManual != null)
                await OnMultipleCardsMovedManual.Invoke(playerId, cardIds, fromZone, toZone);
        });

        _hub.On<string, int, bool>("CardTappedToggled", async (playerId, cardId, isTapped) =>
        {
            if (OnCardTappedToggled != null)
                await OnCardTappedToggled.Invoke(playerId, cardId, isTapped);
        });

        _hub.On<string, int, bool>("CardFlipped", async (playerId, cardId, faceUp) =>
        {
            if (OnCardFlipped != null)
                await OnCardFlipped.Invoke(playerId, cardId, faceUp);
        });

        _hub.On<string, int, string, int>("CounterAdded", async (playerId, cardId, counterType, amount) =>
        {
            if (OnCounterAdded != null)
                await OnCounterAdded.Invoke(playerId, cardId, counterType, amount);
        });

        _hub.On<string, int, string, int>("CounterRemoved", async (playerId, cardId, counterType, amount) =>
        {
            if (OnCounterRemoved != null)
                await OnCounterRemoved.Invoke(playerId, cardId, counterType, amount);
        });

        _hub.On<string, int>("MoraleAdjusted", async (playerId, amount) =>
        {
            if (OnMoraleAdjustedManual != null)
                await OnMoraleAdjustedManual.Invoke(playerId, amount);
        });

        _hub.On<string, int>("TierSet", async (playerId, newTier) =>
        {
            if (OnTierSet != null)
                await OnTierSet.Invoke(playerId, newTier);
        });

        _hub.On<string, string, int>("CardsDrawn", async (playerId, deckType, count) =>
        {
            if (OnCardsDrawn != null)
                await OnCardsDrawn.Invoke(playerId, deckType, count);
        });

        _hub.On<string, string>("DeckShuffled", async (playerId, deckType) =>
        {
            if (OnDeckShuffled != null)
                await OnDeckShuffled.Invoke(playerId, deckType);
        });

        _hub.On<string>("AllUnitsUntapped", async (playerId) =>
        {
            if (OnAllUnitsUntapped != null)
                await OnAllUnitsUntapped.Invoke(playerId);
        });

        _hub.On<string, string>("PhaseAdvanced", async (playerId, newPhase) =>
        {
            if (OnPhaseAdvanced != null)
                await OnPhaseAdvanced.Invoke(playerId, newPhase);
        });

        _hub.On<string, int>("RoundAdvanced", async (playerId, newRound) =>
        {
            if (OnRoundAdvanced != null)
                await OnRoundAdvanced.Invoke(playerId, newRound);
        });

        _hub.On<string, string, string, string>("Ping", async (playerId, targetType, targetId, message) =>
        {
            if (OnPing != null)
                await OnPing.Invoke(playerId, targetType, targetId, message);
        });

        _hub.On<string, string, DateTime>("ChatMessage", async (playerId, message, timestamp) =>
        {
            if (OnChatMessageManual != null)
                await OnChatMessageManual.Invoke(playerId, message, timestamp);
        });

        _hub.On<string, string>("ManualGameStarted", async (player1Id, player2Id) =>
        {
            if (OnManualGameStarted != null)
                await OnManualGameStarted.Invoke(player1Id, player2Id);
        });

        _hub.On<object>("GameStateUpdatedManual", async (gameState) =>
        {
            if (OnGameStateUpdatedManual != null)
                await OnGameStateUpdatedManual.Invoke(gameState);
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
        {
            await _hub.DisposeAsync();
        }
    }
}
