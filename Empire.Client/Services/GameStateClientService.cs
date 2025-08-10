using Empire.Shared.Models;
using Empire.Shared.Models.DTOs;

namespace Empire.Client.Services;

public class GameStateClientService
{
    private GameState? _currentGameState;
    private readonly object _stateLock = new object();

    public event Action? OnStateChanged;

    public GameState? CurrentGameState
    {
        get
        {
            lock (_stateLock)
            {
                return _currentGameState;
            }
        }
    }

    public void UpdateGameState(GameState newState)
    {
        lock (_stateLock)
        {
            _currentGameState = newState;
        }
        OnStateChanged?.Invoke();
    }

    public List<int> GetPlayerHandIds(string playerId)
    {
        lock (_stateLock)
        {
            if (_currentGameState?.PlayerHands?.TryGetValue(playerId, out var handIds) == true)
            {
                return handIds;
            }
            return new List<int>();
        }
    }

    public List<Card> GetPlayerHand(string playerId)
    {
        lock (_stateLock)
        {
            if (_currentGameState?.PlayerHands?.TryGetValue(playerId, out var handIds) == true &&
                _currentGameState?.PlayerDecks?.TryGetValue(playerId, out var deck) == true)
            {
                return deck.Where(c => handIds.Contains(c.CardId)).ToList();
            }
            return new List<Card>();
        }
    }

    public List<BoardCard> GetPlayerBoard(string playerId)
    {
        lock (_stateLock)
        {
            if (_currentGameState?.PlayerBoard?.TryGetValue(playerId, out var board) == true)
            {
                return board;
            }
            return new List<BoardCard>();
        }
    }

    public Card? GetCardById(int cardId)
    {
        lock (_stateLock)
        {
            if (_currentGameState?.PlayerDecks != null)
            {
                foreach (var deck in _currentGameState.PlayerDecks.Values)
                {
                    var card = deck.FirstOrDefault(c => c.CardId == cardId);
                    if (card != null) return card;
                }
            }
            return null;
        }
    }

    public int GetDeckCount(string playerId, string deckType)
    {
        lock (_stateLock)
        {
            // For now, return a mock count since DeckCounts doesn't exist in GameState
            // This should be implemented properly when the server supports it
            return 30; // Default deck size
        }
    }

    public int GetPlayerLifeTotal(string playerId)
    {
        lock (_stateLock)
        {
            if (_currentGameState?.PlayerLifeTotals?.TryGetValue(playerId, out var lifeTotal) == true)
            {
                return lifeTotal;
            }
            return 20; // Default life total
        }
    }

    public string GetCurrentPhase()
    {
        lock (_stateLock)
        {
            return _currentGameState?.CurrentPhase.ToString() ?? "Strategy";
        }
    }

    public string? GetActivePlayer()
    {
        lock (_stateLock)
        {
            // Use PriorityPlayer as the active player for now
            return _currentGameState?.PriorityPlayer ?? _currentGameState?.Player1;
        }
    }

    public string? GetOpponentId(string playerId)
    {
        lock (_stateLock)
        {
            if (_currentGameState == null) return null;
            
            // Return the other player
            if (playerId == _currentGameState.Player1)
                return _currentGameState.Player2;
            else if (playerId == _currentGameState.Player2)
                return _currentGameState.Player1;
            
            return null;
        }
    }

    public bool IsPlayerTurn(string playerId)
    {
        lock (_stateLock)
        {
            var activePlayer = GetActivePlayer();
            return activePlayer == playerId;
        }
    }

    // New methods for game actions
    public void AddCardToHand(string playerId, int cardId)
    {
        lock (_stateLock)
        {
            if (_currentGameState?.PlayerHands?.TryGetValue(playerId, out var hand) == true)
            {
                hand.Add(cardId);
                OnStateChanged?.Invoke();
            }
        }
    }

    public void PlayCardFromHand(string playerId, int cardId)
    {
        lock (_stateLock)
        {
            if (_currentGameState?.PlayerHands?.TryGetValue(playerId, out var hand) == true &&
                _currentGameState?.PlayerBoard?.TryGetValue(playerId, out var board) == true)
            {
                if (hand.Remove(cardId))
                {
                    board.Add(new BoardCard(cardId));
                    OnStateChanged?.Invoke();
                }
            }
        }
    }

    public void ExertCard(string playerId, int cardId)
    {
        lock (_stateLock)
        {
            if (_currentGameState?.PlayerBoard?.TryGetValue(playerId, out var board) == true)
            {
                var boardCard = board.FirstOrDefault(bc => bc.CardId == cardId);
                if (boardCard != null)
                {
                    boardCard.IsExerted = !boardCard.IsExerted;
                    OnStateChanged?.Invoke();
                }
            }
        }
    }

    public void EndTurn()
    {
        lock (_stateLock)
        {
            if (_currentGameState != null)
            {
                // Switch active player using PriorityPlayer
                var currentActive = _currentGameState.PriorityPlayer;
                var opponent = GetOpponentId(currentActive ?? "");
                if (opponent != null)
                {
                    _currentGameState.PriorityPlayer = opponent;
                    
                    // Unexert all cards for the new active player
                    if (_currentGameState.PlayerBoard?.TryGetValue(opponent, out var board) == true)
                    {
                        foreach (var card in board)
                        {
                            card.IsExerted = false;
                        }
                    }
                    
                    OnStateChanged?.Invoke();
                }
            }
        }
    }

    public void ClearState()
    {
        lock (_stateLock)
        {
            _currentGameState = null;
        }
        OnStateChanged?.Invoke();
    }
}
