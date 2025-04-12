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

    public async Task<List<Card>> CreateDeckAsync(List<int> cardIds, string deckType)
    {
        // Get all unique card data from the DB
        var allCardData = _cardDb.GetAllCards()
            .Where(cd => cardIds.Contains(cd.CardID))
            .ToDictionary(cd => cd.CardID, cd => cd); // fast lookup

        var result = new List<Card>();

        foreach (var id in cardIds)
        {
            if (allCardData.TryGetValue(id, out var cd))
            {
                var newCard = new Card
                {
                    CardId = cd.CardID,
                    Name = cd.Name,
                    CardText = cd.CardText,
                    Type = cd.CardType,
                    Faction = cd.Faction,
                    DeckType = deckType,
                    ImagePath = cd.ImageFileName ?? "images/Cards/placeholder.jpg",
                    IsExerted = false,
                    CurrentDamage = 0
                };

                result.Add(newCard);
            }
            else
            {
                Console.WriteLine($"❌ Card ID {id} not found in DB");
            }
        }

        Console.WriteLine($"✅ Hydrated {result.Count} {deckType} cards from {cardIds.Count} requested IDs");
        return await Task.FromResult(result);
    }


}
