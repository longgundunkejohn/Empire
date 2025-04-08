//  File: Empire.Shared/Models/PlayerDeck.cs
using Empire.Shared.Models; // Add this using statement
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Empire.Shared.Models
{
    public class PlayerDeck
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("PlayerName")]
        public string PlayerName { get; set; } = string.Empty;

        [BsonIgnore] //  We don't want to store this directly in the DB
        public List<Card> Cards { get; set; } = new();

        [BsonElement("CivicDeck")]
        public List<int> CivicDeck { get; set; } = new(); // Keep this for now for DB purposes

        [BsonElement("MilitaryDeck")]
        public List<int> MilitaryDeck { get; set; } = new(); // Keep this for now for DB purposes


        public PlayerDeck() { }

        public PlayerDeck(string playerName, List<int> civicDeck, List<int> militaryDeck)
        {
            PlayerName = playerName;
            CivicDeck = civicDeck;
            MilitaryDeck = militaryDeck;
        }
    }
}