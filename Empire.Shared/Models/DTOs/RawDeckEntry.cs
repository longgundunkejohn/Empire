namespace Empire.Shared.Models.DTOs
{
    public class RawDeckEntry
    {
        public int CardId { get; set; }
        public int Count { get; set; }
        public string Player { get; set; } = string.Empty;

        public string DeckType { get; set; } // "Civic" or "Military"
    }
}