using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Empire.Shared.Models; // Add this using statement

public class PlayerDeck
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string PlayerName { get; set; } = string.Empty;

    public List<Card> CivicDeck { get; set; } = new(); // Change to List<Card>
    public List<Card> MilitaryDeck { get; set; } = new(); // Change to List<Card>

    public PlayerDeck() { }

    public PlayerDeck(string playerName, List<Card> civic, List<Card> military) // Change to List<Card>
    {
        PlayerName = playerName;
        CivicDeck = civic;
        MilitaryDeck = military;
    }
}