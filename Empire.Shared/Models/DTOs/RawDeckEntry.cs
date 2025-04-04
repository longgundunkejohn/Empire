namespace Empire.Shared.Models.DTOs
{
    public class RawDeckEntry
    {
        public int CardId { get; set; }
        public string CardName { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
