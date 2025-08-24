using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Empire.Shared.Models
{
    public class GameMove
    {
        public required string PlayerId { get; set; }
        public required string MoveType { get; set; }  // Example: "DrawCard", "PlayCard"
        public int? CardId { get; set; }  // Nullable because not all moves need a card ID
        public int? Value { get; set; }  // Used for life gain/loss
        public bool? IsExerting { get; set; } // 
    }
}
