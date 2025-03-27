using System.Collections.Generic;
using Empire.Shared.Models;
using Empire.Shared.Interfaces;
public class BoardService

{
    public Dictionary<string, List<BoardCard>> PlayerBoards { get; private set; } = new();

    public void PlaceCard(string player, int cardId)
    {
        if (!PlayerBoards.ContainsKey(player)) return;
        PlayerBoards[player].Add(new BoardCard(cardId));
    }

    public BoardService()
    {
        PlayerBoards["Player1"] = new List<BoardCard>();
        PlayerBoards["Player2"] = new List<BoardCard>();
    }


    public void RotateCard(string player, int cardId)
    {
        var card = PlayerBoards[player].Find(c => c.CardId == cardId);
        if (card != null)
        {
            card.Rotate();
        }
    }

    public void RemoveCard(string player, int cardId)
    {
        PlayerBoards[player].RemoveAll(c => c.CardId == cardId);
    }
}
