using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class PlayerDeck
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)] // 👈 Just in case you're dealing with JSON
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string PlayerName { get; set; } = string.Empty;

    public string DeckName { get; set; } = "Untitled"; // 👈 new, optional

    public List<int> CivicDeck { get; set; } = new();
    public List<int> MilitaryDeck { get; set; } = new();

    public PlayerDeck() { }

    public PlayerDeck(string playerName, List<int> civic, List<int> military, string? deckName = null)
    {
        PlayerName = playerName;
        DeckName = deckName ?? "Untitled";
        CivicDeck = civic;
        MilitaryDeck = military;
    }
}
