using Empire.Shared.Models;
using Empire.Server.Services; // <--- This is the key line!

public class GameStateService
{
    private readonly ICardService _cardService;
    private readonly BoardService _boardService;

    public GameState GameState { get; private set; }

    public GameStateService(ICardService cardService, BoardService boardService)
    {
        _cardService = cardService;
        _boardService = boardService;
        GameState = new GameState();
    } 

    public void InitializeGame(string playerId, List<int> civicDeck, List<int> militaryDeck)
    {
        var cards = civicDeck.Select(id => new Card { CardId = id, Type = "Civic" })
            .Concat(militaryDeck.Select(id => new Card { CardId = id, Type = "Military" }))
            .ToList();

        _boardService.InitializePlayer(playerId, cards);
        GameState.PlayerLifeTotals[playerId] = 25;
    }

    public void DrawCard(string playerId, bool isCivic)
    {
        if (!GameState.PlayerDecks.TryGetValue(playerId, out var deck))
            return;

        var subset = isCivic
            ? deck.Where(c => DeckUtils.IsCivicCard(c.CardId)).ToList()
            : deck.Where(c => !DeckUtils.IsCivicCard(c.CardId)).ToList();

        if (!subset.Any()) return;

        var card = subset.First();
        deck.Remove(card);

        _boardService.DrawCard(playerId, card.CardId); // ✅ move to hand
    }

    public void PlayCard(string playerId, int cardId)
    {
        _boardService.MoveToBoard(playerId, cardId);
    }
}
