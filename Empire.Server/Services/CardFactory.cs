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
            var baseCard = await CreateCardFromIdAsync(id);
            if (baseCard != null)
            {
                for (int i = 0; i < count; i++)
                {
                    result.Add(new Card
                    {
                        CardId = baseCard.CardId,
                        Name = baseCard.Name,
                        CardText = baseCard.CardText,
                        Type = baseCard.Type,
                        Faction = baseCard.Faction,
                        IsExerted = false,
                        CurrentDamage = 0,
                        ImagePath = baseCard.ImagePath,
                        DeckType = deckType
                    });
                }
            }
            else
            {
                Console.WriteLine($"❌ Card not found for ID: {id}");
            }
        }

        return result;
    }


}
