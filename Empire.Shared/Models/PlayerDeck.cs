using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class PlayerDeck
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)] // Let Mongo handle this
    public string? Id { get; set; }

    public string PlayerName { get; set; } = string.Empty;

    public List<int> CivicDeck { get; set; } = new();
    public List<int> MilitaryDeck { get; set; } = new();

    public PlayerDeck() { }

    public PlayerDeck(string playerName, List<int> civic, List<int> military)
    {
        PlayerName = playerName;
        CivicDeck = civic;
        MilitaryDeck = military;
    }
}
