using Empire.Shared.Models;

public class BoardClientService
{
    private readonly Dictionary<string, List<BoardCard>> _playerBoards = new();
    private readonly Dictionary<string, List<int>> _playerHands = new();

    public List<BoardCard> GetBoard(string playerId)
    {
        if (!_playerBoards.ContainsKey(playerId))
            _playerBoards[playerId] = new List<BoardCard>();

        return _playerBoards[playerId];
    }

    public List<int> GetHandIds(string playerId)
    {
        if (!_playerHands.ContainsKey(playerId))
            _playerHands[playerId] = new List<int>();

        return _playerHands[playerId];
    }

    public void SetBoard(string playerId, List<BoardCard> cards)
    {
        _playerBoards[playerId] = cards;
    }

    public void SetHand(string playerId, List<int> hand)
    {
        _playerHands[playerId] = hand;
    }

    public void MoveToBoard(string playerId, int cardId)
    {
        if (!_playerHands.ContainsKey(playerId) || !_playerBoards.ContainsKey(playerId))
            return;

        _playerHands[playerId].Remove(cardId);
        _playerBoards[playerId].Add(new BoardCard(cardId));
    }
}
