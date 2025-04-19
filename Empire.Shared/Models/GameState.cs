using Empire.Shared.Models;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;


namespace Empire.Shared.Models
{

    [BsonIgnoreExtraElements]
    public class GameState
    {
        // Explicit ObjectId for Mongo, not using string for GameId
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)] // ✅ Add this line
        public string GameId { get; set; } = string.Empty;
        public Dictionary<string, List<int>> PlayerSealedZones { get; set; } = new();

        public string Player1 { get; set; } = string.Empty;
        public string? Player2 { get; set; }
        public string? InitiativeHolder { get; set; }
        public string? PriorityPlayer { get; set; }

        public GamePhase CurrentPhase { get; set; }

        public GameBoard GameBoardState { get; set; } = new GameBoard();

        public Dictionary<string, List<BoardCard>> PlayerBoard { get; set; } = new();
        public Dictionary<string, List<int>> PlayerHands { get; set; } = new();
        public Dictionary<string, List<Card>> PlayerDecks { get; set; } = new();
        public Dictionary<string, List<int>> PlayerGraveyards { get; set; } = new();
        public List<GameMove> MoveHistory { get; set; } = new();
        public Dictionary<string, int> PlayerLifeTotals { get; set; } = new();
    }
}