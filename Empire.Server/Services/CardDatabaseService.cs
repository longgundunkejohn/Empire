using Empire.Shared.Models;
using Empire.Server.Interfaces;
using MongoDB.Driver;
using Empire.Server.Services;

public class CardDatabaseService : ICardDatabaseService
{
    private readonly IMongoCollection<CardData> _cardCollection;
    private Dictionary<string, CardData> _cardDictionary = new();
    private readonly DeckLoaderService _deckLoader;

    public CardDatabaseService(IMongoDbService mongo, DeckLoaderService deckLoader)
    {
        _deckLoader = deckLoader;
        var db = mongo.GetDatabase();
        _cardCollection = db.GetCollection<CardData>("Cards");
        LoadCards();
    }


    private void LoadCards()
    {
        var cards = _cardCollection.Find(_ => true).ToList();

        foreach (var card in cards)
        {
            card.ImageFileName = _deckLoader.GetImagePath(card.CardID);
        }

        Console.WriteLine($"Loaded {cards.Count} cards from MongoDB!");
        _cardDictionary = cards.ToDictionary(c => c.CardID.ToString(), c => c);
    }



    public IEnumerable<CardData> GetAllCards() => _cardDictionary.Values;

    public CardData? GetCardById(string id)
    {
        return _cardDictionary.TryGetValue(id, out var card) ? card : null;
    }
}
