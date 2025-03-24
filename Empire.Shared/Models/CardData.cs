using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class CardData
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("CardID")]
    public int CardID { get; set; }

    [BsonElement("Name")]
    public string Name { get; set; }

    [BsonElement("CardText")]
    public string CardText { get; set; }

    [BsonElement("CardType")]
    public string CardType { get; set; }

    [BsonElement("Tier")]
    public string Tier { get; set; }

    [BsonElement("Cost")]
    public int Cost { get; set; }

    [BsonElement("Attack")]
    public int Attack { get; set; }

    [BsonElement("Defence")]
    public int Defence { get; set; }

    [BsonElement("Unique")]
    public string Unique { get; set; }

    [BsonElement("Faction")]
    public string Faction { get; set; }

    public string ImageFileName { get; set; } = string.Empty;



}
