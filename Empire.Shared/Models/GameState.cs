using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Empire.Shared.Models
{
    public class GameState
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)] // ✅ NOT ObjectId
        public string GameId { get; set; } = Guid.NewGuid().ToString();


        public string Player1 { get; set; }
        public string Player2 { get; set; }
        public string InitiativeHolder { get; set; }
        public string PriorityPlayer { get; set; }

        public GamePhase CurrentPhase { get; set; }

        public GameBoard GameBoardState { get; set; } = new GameBoard();
        public Dictionary<string, List<int>> PlayerBoard { get; set; } = new();
         
        public Dictionary<string, List<int>> PlayerHands { get; set; } = new();
        public Dictionary<string, PlayerDeck> PlayerDecks { get; set; } = new();
        public Dictionary<string, List<int>> PlayerGraveyards { get; set; } = new();

        public List<GameMove> MoveHistory { get; set; } = new();
    }
}
