using Empire.Shared.Models;
using Empire.Server.Interfaces; // ✅ needed
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Empire.Server.Services;

public class CardSanitizerService
{
    private readonly IMongoCollection<BsonDocument> _rawCards;
    private readonly IMongoCollection<CardData> _cleanCards;
    private readonly ILogger<CardSanitizerService> _logger;
    private readonly string _imagePath;

    public CardSanitizerService(IMongoDbService mongo, ILogger<CardSanitizerService> logger, IWebHostEnvironment env)
    {
        var db = mongo.GetDatabase();
        _rawCards = db.GetCollection<BsonDocument>("Cards");
        _cleanCards = db.GetCollection<CardData>("CardsForGame");
        _logger = logger;
        _imagePath = Path.Combine(env.WebRootPath, "images");
    }

    public async Task<int> RunAsync(bool clearBeforeInsert = true)
    {
        if (clearBeforeInsert)
        {
            await _cleanCards.DeleteManyAsync(_ => true);
            _logger.LogInformation("Cleared CardsForGame before sanitization.");
        }

        var raw = await _rawCards.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        int inserted = 0;

        foreach (var doc in raw)
        {
            try
            {
                var card = Sanitize(doc);
                if (card != null)
                {
                    await _cleanCards.InsertOneAsync(card);
                    inserted++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipping card due to error.");
            }
        }

        _logger.LogInformation("Sanitization complete. Inserted {Count} cards into CardsForGame.", inserted);
        return inserted;
    }

    private CardData? Sanitize(BsonDocument doc)
    {
        var isValid = true;
        var card = new CardData();

        if (!doc.Contains("CardID") || !doc["CardID"].IsInt32)
            return null;

        card.CardID = doc["CardID"].AsInt32;

        // ✅ Fix: proper conversion
        card.Id = doc.GetValue("_id", ObjectId.GenerateNewId()).AsObjectId;

        card.Cost = TryParseCost(doc.GetValue("Cost", BsonNull.Value), ref isValid);
        card.Attack = TryParseInt(doc, "Attack", ref isValid);
        card.Defence = TryParseInt(doc, "Defence", ref isValid);

        card.Name = SanitizeString(doc.GetValue("Name", "").ToString(), $"BugTemplate_{card.CardID}");
        card.CardText = SanitizeString(doc.GetValue("CardText", "").ToString());
        card.CardType = SanitizeString(doc.GetValue("CardType", "").ToString());
        card.Tier = SanitizeString(doc.GetValue("Tier", "").ToString());

        card.Unique = ValidateYesNo(doc.GetValue("Unique", "No").ToString(), ref isValid);
        card.Faction = ValidateYesNo(doc.GetValue("Faction", "No").ToString(), ref isValid);

        card.ImageFileName = FindImage(card.CardID);

        return isValid ? card : null;
    }

    private int TryParseCost(BsonValue val, ref bool isValid)
    {
        if (val.IsInt32) return val.AsInt32;
        if (val.IsString)
        {
            var str = val.AsString.ToLower();
            if (str == "villager" || str == "settlement") return 0;
        }

        isValid = false;
        return -1;
    }

    private int TryParseInt(BsonDocument doc, string field, ref bool isValid)
    {
        var val = doc.GetValue(field, BsonNull.Value);
        if (val.IsInt32) return val.AsInt32;
        isValid = false;
        return -1;
    }

    private string SanitizeString(string? input, string fallback = "-invalid-", int maxLen = 100)
    {
        if (string.IsNullOrWhiteSpace(input)) return fallback;
        return input.Length > maxLen ? input.Substring(0, maxLen) : input;
    }

    private string ValidateYesNo(string val, ref bool isValid)
    {
        return val switch
        {
            "Yes" or "No" => val,
            _ => SetInvalid(ref isValid, "No")
        };
    }

    private static string SetInvalid(ref bool flag, string fallback)
    {
        flag = false;
        return fallback;
    }

    private string FindImage(int cardId)
    {
        var match = Directory.GetFiles(_imagePath, "*.jpg")
            .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).StartsWith(cardId.ToString()));

        return match != null
            ? $"images/{Path.GetFileName(match)}"
            : "images/Cards/placeholder.jpg";
    }
}
