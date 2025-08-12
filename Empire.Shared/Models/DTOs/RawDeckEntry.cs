namespace Empire.Shared.Models.DTOs
{
    public class RawDeckEntry
    {
        public int CardId { get; set; }
        public int Count { get; set; }
        public string DeckType { get; set; } = string.Empty;
        public string CardName { get; set; } = string.Empty;
        public string Player { get; set; } = string.Empty; // Legacy property for backward compatibility
    }
}
