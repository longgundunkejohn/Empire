using System;
using Empire.Shared.Models;
using Empire.Server.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Empire.Server.Services;

public class CardSanitizerServiceV2
{
    private readonly IMongoCollection<BsonDocument> _source;
    private readonly IMongoCollection<CardData> _target;
    private readonly ILogger<CardSanitizerServiceV2> _logger;
    private readonly string _cardsPath;

    public CardSanitizerServiceV2(IMongoDbService mongo, ILogger<CardSanitizerServiceV2> logger, IWebHostEnvironment env)
    {
        _source = mongo.CardDatabase.GetCollection<BsonDocument>("Cards");
        _target = mongo.CardDatabase.GetCollection<CardData>("CardsForGame");
        _logger = logger;
        _cardsPath = Path.Combine(env.WebRootPath, "images", "Cards");
    }

    public async Task<int> RunAsync(bool clearBeforeInsert = true)
    {
        if (clearBeforeInsert)
        {
            await _target.DeleteManyAsync(_ => true);
            _logger.LogInformation("🧼 Cleared CardsForGame collection.");
        }

        var rawCards = await _source.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        int inserted = 0, skipped = 0;
        var seenCardIds = new HashSet<int>();

        foreach (var raw in rawCards)
        {
            try
            {
                var card = Sanitize(raw);
                if (card == null)
                {
                    skipped++;
                    continue;
                }

                if (!seenCardIds.Add(card.CardID))
                {
                    _logger.LogWarning("⚠️ Duplicate CardID found in source: {CardID}. Skipping.", card.CardID);
                    skipped++;
                    continue;
                }

                await _target.InsertOneAsync(card);
                inserted++;
            }
            catch (MongoWriteException mwx) when (mwx.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                _logger.LogWarning("🚫 Duplicate key on insert: {Message}", mwx.Message);
                skipped++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "🚫 Failed to sanitize card. Skipping.");
                skipped++;
            }
        }

        _logger.LogInformation("✅ Sanitization complete. Inserted {Inserted} cards, Skipped {Skipped}.", inserted, skipped);
        return inserted;
    }

    private CardData Sanitize(BsonDocument doc)
    {
        var card = new CardData
        {
            Id = ObjectId.GenerateNewId(),
            CardID = doc.GetValue("CardID", -1).ToInt32(),
            Cost = TryParseCost(doc.GetValue("Cost", BsonNull.Value)),
            Attack = TryParseInt(doc, "Attack"),
            Defence = TryParseInt(doc, "Defence"),
            Name = SanitizeString(doc.GetValue("Name", null)?.ToString(), $"Bug_{doc.GetValue("CardID", -1)}"),
            CardText = SanitizeMultiline(doc.GetValue("CardText", "").ToString()),
            CardType = SanitizeString(doc.GetValue("CardType", "Unknown").ToString()),
            Tier = SanitizeString(doc.GetValue("Tier", "-").ToString()),
            Unique = CoerceYesNo(doc.GetValue("Unique", "No").ToString()),
            Faction = CoerceYesNo(doc.GetValue("Faction", "No").ToString()),
            ImageFileName = FindImage(doc.GetValue("CardID", -1).ToInt32())
        };

        return card;
    }

    private string FindImage(int cardId)
    {
        var file = Path.Combine(_cardsPath, $"{cardId}.jpg");
        return File.Exists(file)
            ? $"images/Cards/{cardId}.jpg"
            : "images/Cards/placeholder.jpg";
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

    private string SanitizeString(string? input, string fallback = "Unknown", int maxLen = 100)
    {
        if (string.IsNullOrWhiteSpace(input)) return fallback;
        var cleaned = input.Trim();
        return cleaned.Length > maxLen ? cleaned.Substring(0, maxLen) : cleaned;
    }

    private string SanitizeMultiline(string input)
    {
        return string.Join(" ", input.Split('\n', '\r')).Trim();
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
}
