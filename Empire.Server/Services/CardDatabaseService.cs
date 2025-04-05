using Empire.Shared.Models;
using Empire.Server.Interfaces;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;

public class CardDatabaseService : ICardDatabaseService
{
    private readonly IMongoCollection<CardData> _cardCollection;
    private readonly ILogger<CardDatabaseService> _logger;
    private Dictionary<string, CardData> _cardDictionary = new();
    private readonly Dictionary<int, string> _cardImagePaths;

    public CardDatabaseService(IMongoDbService mongo, ILogger<CardDatabaseService> logger)
    {
        _logger = logger;

        try
        {
            var db = mongo.GetDatabase();
            _cardCollection = db.GetCollection<CardData>("Cards");

            _cardImagePaths = LoadImageMappings(); // 👈 New helper
            _logger.LogInformation("CardDatabaseService connected to MongoDB collection.");
            LoadCards();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize CardDatabaseService.");
            throw;
        }
    }

    private Dictionary<int, string> LoadImageMappings()
    {
        var result = new Dictionary<int, string>();
        var imagePath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "images");

        if (!Directory.Exists(imagePath))
            return result;

        foreach (var file in Directory.GetFiles(imagePath, "*.jpg"))
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            if (int.TryParse(fileName.Split(' ')[0], out int cardId))
            {
                result[cardId] = $"images/{fileName}.jpg";
            }
        }

        return result;
    }

    private void LoadCards()
    {
        try
        {
            _logger.LogInformation("Loading cards from MongoDB...");
            var cards = _cardCollection.Find(_ => true).ToList();

            foreach (var card in cards)
            {
                card.ImageFileName = _cardImagePaths.TryGetValue(card.CardID, out var path)
                    ? path
                    : "images/Cards/placeholder.jpg";
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
