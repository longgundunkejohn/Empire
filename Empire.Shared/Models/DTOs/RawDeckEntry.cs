namespace Empire.Shared.Models.DTOs
{
    public class RawDeckEntry
    {
        public int CardId { get; set; }
        public int Count { get; set; }
        public string DeckType { get; set; } // "Civic" or "Military"
    }
}