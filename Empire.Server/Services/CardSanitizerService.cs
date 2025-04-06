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

    private CardData Sanitize(BsonDocument doc)
    {
        var card = new CardData();

        // Required ID
        card.CardID = doc.Contains("CardID") && doc["CardID"].IsInt32
            ? doc["CardID"].AsInt32
            : -1;

        card.Id = doc.GetValue("_id", ObjectId.GenerateNewId()).IsObjectId
            ? doc["_id"].AsObjectId
            : ObjectId.GenerateNewId();

        // Safe parsing with fallbacks
        card.Cost = TryParseCost(doc.GetValue("Cost", BsonNull.Value));
        card.Attack = TryParseInt(doc, "Attack");
        card.Defence = TryParseInt(doc, "Defence");

        card.Name = SanitizeString(doc.GetValue("Name", null)?.ToString(), $"Bug_{card.CardID}");
        card.CardText = SanitizeMultiline(doc.GetValue("CardText", "").ToString());
        card.CardType = SanitizeString(doc.GetValue("CardType", "Unknown").ToString());
        card.Tier = SanitizeString(doc.GetValue("Tier", "-").ToString());

        card.Unique = CoerceYesNo(doc.GetValue("Unique", "No").ToString());
        card.Faction = CoerceYesNo(doc.GetValue("Faction", "No").ToString());

        card.ImageFileName = FindImage(card.CardID);

        return card;
    }


    private int TryParseCost(BsonValue val)
    {
        if (val.IsInt32) return val.AsInt32;
        if (val.IsString)
        {
            var str = val.AsString.ToLower();
            if (str == "villager" || str == "settlement") return 0;
            if (int.TryParse(str, out var parsed)) return parsed;
        }
        return 0;
    }

    private int TryParseInt(BsonDocument doc, string field)
    {
        var val = doc.GetValue(field, BsonNull.Value);
        return val.IsInt32 ? val.AsInt32 : 1;
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
    private string CoerceYesNo(string? input)
    {
        return input?.Trim().ToLower() switch
        {
            "yes" => "Yes",
            "no" => "No",
            _ => "No"
        };
    }

    private string SanitizeMultiline(string input)
    {
        return string.Join(" ", input.Split('\n', '\r')).Trim();
    }

}
