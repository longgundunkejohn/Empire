using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Empire.Shared.Models.DTOs; // Add this

namespace Empire.Shared.Models
{
    public class GameState
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string GameId { get; set; } = Guid.NewGuid().ToString();

        public string Player1 { get; set; }
        public string? Player2 { get; set; }
        public string? InitiativeHolder { get; set; }
        public string? PriorityPlayer { get; set; }

        public GamePhase CurrentPhase { get; set; }

        public GameBoard GameBoardState { get; set; } = new GameBoard();

        public Dictionary<string, List<BoardCard>> PlayerBoard { get; set; } = new();

        public Dictionary<string, List<int>> PlayerHands { get; set; } = new();
        public Dictionary<string, PlayerDeck> PlayerDecks { get; set; } = new();
        public Dictionary<string, List<int>> PlayerGraveyards { get; set; } = new();

        public List<GameMove> MoveHistory { get; set; } = new();

        public Dictionary<string, int> PlayerLifeTotals { get; set; } = new();

        // New properties to store deck data using RawDeckEntry
        public List<RawDeckEntry> Player1Deck { get; set; } = new();
        public List<RawDeckEntry>? Player2Deck { get; set; }
    }
}