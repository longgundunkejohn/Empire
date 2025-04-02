using System;

namespace Empire.Shared.Models
{
    public class Card
    {
        public int CardId { get; set; }
        public string Name { get; set; }
        public string CardText { get; set; }
        public string Type { get; set; }
        public string Faction { get; set; }
        public bool IsExerted { get; set; } = false;
        public int CurrentDamage { get; set; } = 0;

        public string ImagePath => $"images/Cards/{CardId} - {Name}.jpg";

        public Card() { } // 🔧 Needed for deserialization

        public Card(CardData data)
        {
            CardId = data.CardID;
            Name = data.Name;
            CardText = data.CardText;
            Type = data.CardType;
            Faction = data.Faction;
        }
    }
}
