namespace Empire.Shared.Models
{
    public class CardData
    {
        public int CardID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CardText { get; set; } = string.Empty;
        public string CardType { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty;
        public int Cost { get; set; }
        public int Attack { get; set; }
        public int Defence { get; set; }
        public string Unique { get; set; } = string.Empty;
        public string Faction { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageFileName { get; set; } = string.Empty;
    }
}
