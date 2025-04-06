using Empire.Shared.Models;
using Empire.Server.Interfaces;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System.IO;

public class CardDatabaseService : ICardDatabaseService
{
    private readonly ILogger<CardDatabaseService> _logger;
    private readonly Dictionary<int, string> _cardImagePaths;
    private Dictionary<string, CardData> _cardDictionary = new();

    private readonly IMongoCollection<BsonDocument> _rawCollection;

    public CardDatabaseService(IMongoDbService mongo, ILogger<CardDatabaseService> logger)
    {
        _logger = logger;

        try
        {
            var db = mongo.GetDatabase();
            _rawCollection = db.GetCollection<BsonDocument>("Cards");

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
            var documents = _rawCollection.Find(_ => true).ToList();

            foreach (var doc in documents)
            {
                bool isValid = true;
                var card = new CardData();

                try
                {
                    card.Id = doc.GetValue("_id", ObjectId.Empty).AsObjectId;
                    card.CardID = doc.GetValue("CardID", -1).ToInt32();

                    var costValue = doc.GetValue("Cost", BsonNull.Value);
                    if (costValue.IsInt32)
                    {
                        card.Cost = costValue.AsInt32;
                    }
                    else if (costValue.IsString)
                    {
                        var costStr = costValue.AsString.Trim().ToLower();
                        if (costStr == "villager" || costStr == "settlement")
                            card.Cost = 0;
                        else
                        {
                            card.Cost = -1;
                            isValid = false;
                        }
                    }
                    else
                    {
                        card.Cost = -1;
                        isValid = false;
                    }

                    card.Attack = doc.GetValue("Attack", -1).ToInt32();
                    card.Defence = doc.GetValue("Defence", -1).ToInt32();

                    card.Name = SanitizeString(doc.GetValue("Name", "").ToString(), $"bugtemplatename_{card.CardID}");
                    card.CardText = SanitizeString(doc.GetValue("CardText", "").ToString());
                    card.CardType = SanitizeString(doc.GetValue("CardType", "").ToString());
                    card.Tier = SanitizeString(doc.GetValue("Tier", "").ToString());

                    card.Unique = doc.GetValue("Unique", "No").ToString();
                    card.Faction = doc.GetValue("Faction", "No").ToString();

                    if (card.Cost < 0 || card.Attack < 0 || card.Defence < 0)
                        isValid = false;

                    if (!IsValidYesNo(card.Unique) || !IsValidYesNo(card.Faction))
                        isValid = false;

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
                        _rawCollection.DeleteOne(Builders<BsonDocument>.Filter.Eq("_id", card.Id));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing a card. It will be skipped.");
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
