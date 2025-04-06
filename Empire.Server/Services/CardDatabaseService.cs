using Empire.Shared.Models;
using Empire.Server.Interfaces;
using MongoDB.Driver;

public class CardGameDatabaseService : ICardDatabaseService
{
    private readonly IMongoCollection<CardData> _cards;

    public CardGameDatabaseService(IMongoDbService mongo)
    {
        var db = mongo.GetDatabase();
        _cards = db.GetCollection<CardData>("CardsForGame");
    }

    public IEnumerable<CardData> GetAllCards()
    {
        return _cards.Find(_ => true).ToList();
    }

    public CardData? GetCardById(string id)
    {
        if (!int.TryParse(id, out var parsedId)) return null;
        return _cards.Find(c => c.CardID == parsedId).FirstOrDefault();
    }
}
