using Empire.Shared.Models;
using Empire.Server.Interfaces;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

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
                bool isValid = true;

                // COST: Allow "Villager"/"Settlement" to mean 0
                if (card.Cost < 0)
                {
                    // Handle potential string field sneak-ins (legacy data)
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

                // GENERIC FIELD VALIDATIONS
                if (string.IsNullOrWhiteSpace(card.Name) || card.Name.Length > 100)
                {
                    card.Name = $"bugtemplatename_{card.CardID}";
                }

                if (string.IsNullOrWhiteSpace(card.CardText)) card.CardText = "-blank- -nonfunctional-";
                if (string.IsNullOrWhiteSpace(card.CardType)) card.CardType = "-blank- -nonfunctional-";
                if (string.IsNullOrWhiteSpace(card.Tier)) card.Tier = "-blank- -nonfunctional-";

                if (card.Cost < 0 || card.Attack < 0 || card.Defence < 0)
                    isValid = false;

                if (!IsValidYesNo(card.Unique) || !IsValidYesNo(card.Faction))
                    isValid = false;

                // Image fallback
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

    private bool IsValidYesNo(string? value)
    {
        return value is "Yes" or "No";
    }

}
