using Empire.Shared.Models;
using Empire.Server.Interfaces;
using MongoDB.Driver;
using Empire.Server.Services;
using Microsoft.Extensions.Logging;

public class CardDatabaseService : ICardDatabaseService
{
    private readonly IMongoCollection<CardData> _cardCollection;
    private readonly DeckLoaderService _deckLoader;
    private readonly ILogger<CardDatabaseService> _logger;
    private Dictionary<string, CardData> _cardDictionary = new();

    public CardDatabaseService(IMongoDbService mongo, DeckLoaderService deckLoader, ILogger<CardDatabaseService> logger)
    {
        _logger = logger;
        _deckLoader = deckLoader;

        try
        {
            var db = mongo.GetDatabase();
            _cardCollection = db.GetCollection<CardData>("Cards");
            _logger.LogInformation("CardDatabaseService connected to MongoDB collection.");
            LoadCards();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize CardDatabaseService.");
            throw;
        }
    }

    private void LoadCards()
    {
        try
        {
            _logger.LogInformation("Loading cards from MongoDB...");
            var cards = _cardCollection.Find(_ => true).ToList();

            foreach (var card in cards)
            {
                card.ImageFileName = _deckLoader.GetImagePath(card.CardID);
            }

            _cardDictionary = cards.ToDictionary(c => c.CardID.ToString(), c => c);
            _logger.LogInformation("Loaded {CardCount} cards into dictionary.", _cardDictionary.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while loading cards from MongoDB.");
            throw;
        }
    }

    public IEnumerable<CardData> GetAllCards() => _cardDictionary.Values;

    public CardData? GetCardById(string id)
    {
        if (_cardDictionary.TryGetValue(id, out var card))
        {
            _logger.LogDebug("Retrieved card {CardId}: {CardName}", id, card.Name);
            return card;
        }

        _logger.LogWarning("Card with ID {CardId} not found.", id);
        return null;
    }
}
