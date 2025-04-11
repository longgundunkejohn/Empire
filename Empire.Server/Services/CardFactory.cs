using Empire.Server.Services;
using Empire.Shared.Models;

public class CardFactory
{
    private readonly ICardDatabaseService _cardDb;
    private readonly DeckLoaderService _deckLoader;

    public CardFactory(ICardDatabaseService cardDb, DeckLoaderService deckLoader)
    {
        _cardDb = cardDb;
        _deckLoader = deckLoader;
    }

    public Task<Card?> CreateCardFromIdAsync(int id)
    {
        var data = _cardDb.GetCardById(id); // More efficient than GetAllCards().First()
        if (data == null)
            return Task.FromResult<Card?>(null);

        var hydratedCard = new Card
        {
            CardId = data.CardID,
            Name = data.Name,
            CardText = data.CardText,
            Type = data.CardType,
            Faction = data.Faction,
            CurrentDamage = 0,
            IsExerted = false,
            ImagePath = _deckLoader.GetImagePath(data.CardID)
        };

        return Task.FromResult<Card?>(hydratedCard);
    }

    public async Task<List<Card>> CreateDeckAsync(List<(int CardId, int Count)> deckList, string deckType)
    {
        var result = new List<Card>();

        foreach (var (id, count) in deckList)
        {
            var card = await CreateCardFromIdAsync(id);
            if (card != null)
            {
                for (int i = 0; i < count; i++)
                {
                    result.Add(new Card
                    {
                        CardId = card.CardId,
                        Name = card.Name,
                        CardText = card.CardText,
                        Type = card.Type,
                        Faction = card.Faction,
                        IsExerted = false,
                        CurrentDamage = 0,
                        ImagePath = card.ImagePath,
                        DeckType = deckType // 🧠 This is what you were missing
                    });
                }
            }
        }

        return result;
    }

}
