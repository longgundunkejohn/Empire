using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MongoDB.Bson.Serialization.Attributes;
namespace Empire.Shared.Models
{
    public class CardData
    {
        [BsonElement("Card ID")]
        public int CardId { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("Card text")]
        public string CardText { get; set; }

        [BsonElement("Card type")]
        public string CardType { get; set; }

        [BsonElement("Tier")]
        public string Tier { get; set; }

        [BsonElement("Cost")]
        public int Cost { get; set; }

        [BsonElement("Attack")]
        public int Attack { get; set; }

        [BsonElement("Defence")]
        public int Defence { get; set; }

        [BsonElement("Unique")]
        public string Unique { get; set; }

        [BsonElement("Faction")]
        public string Faction { get; set; }
    }

}