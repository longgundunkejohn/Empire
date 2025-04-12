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
            var cardData = await CreateCardFromIdAsync(id);

            if (cardData == null)
            {
                Console.WriteLine($"❌ Card not found for ID: {id}");
                continue;
            }

            Console.WriteLine($"🔧 Hydrating card {cardData.CardId} x{count} ({cardData.Name})");

            for (int i = 0; i < count; i++)
            {
                // 🔁 Create a unique instance per copy
                var cardInstance = new Card
                {
                    CardId = cardData.CardId,
                    Name = cardData.Name,
                    CardText = cardData.CardText,
                    Type = cardData.Type,
                    Faction = cardData.Faction,
                    IsExerted = false,
                    CurrentDamage = 0,
                    ImagePath = cardData.ImagePath,
                    DeckType = deckType
                };

                result.Add(cardInstance);
            }
        }

        Console.WriteLine($"✅ Created {result.Count} cards for deck type: {deckType}");

        var grouped = result
            .GroupBy(c => c.CardId)
            .Select(g => $"🃏 {g.First().Name} (ID {g.Key}): {g.Count()} copies");

        Console.WriteLine("🧾 Final Card Breakdown:");
        foreach (var line in grouped)
        {
            Console.WriteLine($"   {line}");
        }

        return result;
    }

}
