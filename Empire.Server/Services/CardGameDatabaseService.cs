using Empire.Shared.Models;
using Empire.Server.Interfaces;
using MongoDB.Driver;

public class CardGameDatabaseService : ICardDatabaseService
{

    private readonly IMongoCollection<CardData> _cards;

    public CardGameDatabaseService(IMongoDbService mongo)
    {
        _cards = mongo.CardDatabase.GetCollection<CardData>("CardsForGame");
    }

    public IEnumerable<CardData> GetAllCards() =>
        _cards.Find(_ => true).ToList();

    public CardData? GetCardById(int id)
    {
        return _cards.Find(c => c.CardID == id).FirstOrDefault();
    }
    public async Task<List<Card>> GetDeckCards(List<int> cardIds)
{
    var allCardData = GetAllCards()
        .Where(cd => cardIds.Contains(cd.CardID))
        .ToDictionary(cd => cd.CardID, cd => cd); // for fast lookup

    var result = new List<Card>();

    foreach (var id in cardIds)
    {
        if (allCardData.TryGetValue(id, out var cd))
        {
            result.Add(new Card
            {
                CardId = cd.CardID,
                Name = cd.Name,
                CardText = cd.CardText,
                Faction = cd.Faction,
                Type = cd.CardType,
                ImagePath = cd.ImageFileName ?? "images/Cards/placeholder.jpg",
                IsExerted = false,
                CurrentDamage = 0
            });
        }
        else
        {
            Console.WriteLine($"❌ Card ID {id} not found in DB");
        }
    }

    Console.WriteLine($"✅ Hydrated {result.Count} cards from list of {cardIds.Count} IDs");
    return await Task.FromResult(result);
}

}


