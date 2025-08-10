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
            if (_currentGameState?.PlayerBoards?.TryGetValue(playerId, out var board) == true)
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
            if (_currentGameState?.DeckCounts?.TryGetValue(playerId, out var deckCounts) == true)
            {
                return deckCounts.TryGetValue(deckType, out var count) ? count : 0;
            }
            return 0;
        }
    }

    public int GetPlayerLifeTotal(string playerId)
    {
        lock (_stateLock)
        {
            var player = _currentGameState?.Players?.FirstOrDefault(p => p.PlayerId == playerId);
            return player?.LifeTotal ?? 20; // Default life total
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
            return _currentGameState?.ActivePlayerId;
        }
    }

    public string? GetOpponentId(string playerId)
    {
        lock (_stateLock)
        {
            if (_currentGameState?.Players == null) return null;
            
            var opponent = _currentGameState.Players.FirstOrDefault(p => p.PlayerId != playerId);
            return opponent?.PlayerId;
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
                _currentGameState?.PlayerBoards?.TryGetValue(playerId, out var board) == true)
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
            if (_currentGameState?.PlayerBoards?.TryGetValue(playerId, out var board) == true)
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
                // Switch active player
                var currentActive = _currentGameState.ActivePlayerId;
                var opponent = GetOpponentId(currentActive ?? "");
                if (opponent != null)
                {
                    _currentGameState.ActivePlayerId = opponent;
                    
                    // Unexert all cards for the new active player
                    if (_currentGameState.PlayerBoards?.TryGetValue(opponent, out var board) == true)
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
