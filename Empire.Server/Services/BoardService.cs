using Empire.Shared.Models;
using System.Collections.Generic;

namespace Empire.Server.Services

{
    public class BoardService
    {
        // 🧠 Core data store
        private readonly Dictionary<string, List<BoardCard>> _playerBoards = new();
        private readonly Dictionary<string, List<int>> _playerHands = new();

        // ✅ Init player zone storage
        public void InitializePlayer(string playerId, List<Card> cards)
        {
            if (!_playerBoards.ContainsKey(playerId))
                _playerBoards[playerId] = new List<BoardCard>();

            if (!_playerHands.ContainsKey(playerId))
                _playerHands[playerId] = new List<int>();
            // set up dicts if not present

            // Default behavior: place all cards into hand
            _playerHands[playerId] = cards.Select(c => c.CardId).ToList();
            _playerBoards[playerId] = new List<BoardCard>();
        }


        // ✅ Read zones
        public List<BoardCard> GetBoard(string playerId) =>
            _playerBoards.TryGetValue(playerId, out var board) ? board : new List<BoardCard>();

        public List<int> GetHandIds(string playerId) =>
            _playerHands.TryGetValue(playerId, out var hand) ? hand : new List<int>();

        // ✅ Move card from hand to board
        public void MoveToBoard(string playerId, int cardId)
        {
            if (!_playerHands.ContainsKey(playerId) || !_playerBoards.ContainsKey(playerId)) return;

            _playerHands[playerId].Remove(cardId);
            _playerBoards[playerId].Add(new BoardCard(cardId));
        }

        // ✅ Used during draw
        public void DrawCard(string playerId, int cardId)
        {
            if (!_playerHands.ContainsKey(playerId))
                _playerHands[playerId] = new List<int>();

            _playerHands[playerId].Add(cardId);
        }

        // ✅ Overwrite board state (e.g. SignalR updates)
        public void SetBoard(string playerId, List<BoardCard> newBoard)
        {
            _playerBoards[playerId] = newBoard;
        }
    }
}
