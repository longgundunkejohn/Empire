namespace Empire.Shared.Models.DTOs
{
    using MongoDB.Bson.Serialization.Attributes;

    [BsonIgnoreExtraElements]
    public class RawDeckEntry
    {
        public int CardId { get; set; }
        public int Count { get; set; }
        public string DeckType { get; set; } = string.Empty;
        public string Player { get; set; } = string.Empty;
    }

}