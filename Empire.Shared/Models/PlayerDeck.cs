using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Empire.Shared.Models
{
    public class PlayerDeck
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("PlayerName")]
        public string PlayerName { get; set; } = string.Empty;

        [BsonElement("CivicDeck")]
        public List<int> CivicDeck { get; set; } = new();

        [BsonElement("MilitaryDeck")]
        public List<int> MilitaryDeck { get; set; } = new();

        public PlayerDeck() { }

        public PlayerDeck(string playerName, List<int> civicDeck, List<int> militaryDeck)
        {
            PlayerName = playerName;
            CivicDeck = civicDeck;
            MilitaryDeck = militaryDeck;
        }
    }
}
