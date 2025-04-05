using Empire.Shared.Models;
using Empire.Server.Interfaces;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System.IO;

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

            _cardImagePaths = LoadImageMappings();
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
                bool isValid = true;

                // Handle legacy string values in Cost field
                if (card.Cost < 0)
                {
                    var rawDoc = _cardCollection.Find(c => c.CardID == card.CardID).FirstOrDefault();
                    if (rawDoc != null)
                    {
                        var rawCost = rawDoc.ToBsonDocument().GetValue("Cost", BsonNull.Value);
                        if (rawCost.IsString)
                        {
                            string val = rawCost.AsString.Trim().ToLower();
                            if (val == "villager" || val == "settlement")
                            {
                                card.Cost = 0;
                            }
                            else
                            {
                                card.Cost = -1;
                                isValid = false;
                            }
                        }
                    }
                }

                // Sanitize fields
                card.Name = SanitizeString(card.Name, $"bugtemplatename_{card.CardID}");
                card.CardText = SanitizeString(card.CardText);
                card.CardType = SanitizeString(card.CardType);
                card.Tier = SanitizeString(card.Tier);

                if (card.Cost < 0 || card.Attack < 0 || card.Defence < 0)
                    isValid = false;

                if (!IsValidYesNo(card.Unique) || !IsValidYesNo(card.Faction))
                    isValid = false;

                // Set image path or fallback
                card.ImageFileName = _cardImagePaths.TryGetValue(card.CardID, out var path)
                    ? path
                    : "images/Cards/placeholder.jpg";

                if (isValid)
                {
                    _cardDictionary[card.CardID.ToString()] = card;
                }
                else
                {
                    _logger.LogWarning("Card {CardID} is invalid and will be removed.", card.CardID);
                    _cardCollection.DeleteOne(c => c.Id == card.Id);
                }
            }

            _logger.LogInformation("Loaded {CardCount} valid cards into dictionary.", _cardDictionary.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while loading cards from MongoDB.");
            throw;
        }
    }

    private string SanitizeString(string? input, string fallback = "-blank- -nonfunctional-", int maxLength = 100)
    {
        if (string.IsNullOrWhiteSpace(input)) return fallback;
        return input.Length > maxLength ? input.Substring(0, maxLength) : input;
    }

    private bool IsValidYesNo(string? value)
    {
        return value is "Yes" or "No";
    }

    // ✅ Interface Implementation
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
