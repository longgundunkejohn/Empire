namespace Empire.Shared.Models.DTOs
{
    public class DeckCsvEntry
    {
        public int CardId { get; set; }
        public string DeckType { get; set; } = string.Empty; // Must be "Civic" or "Military"
    }
}
