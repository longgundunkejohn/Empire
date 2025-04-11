namespace Empire.Shared.DTOs
{
    public class GameStartRequest
    {
        public string Player1 { get; set; } = string.Empty;
        public string DeckId { get; set; } = string.Empty; // ⬅️ now we're referencing a specific deck
    }
}
