using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Empire.Shared.Models
{
    public static class DeckUtils
    {
        public static bool IsCivicCard(int cardId)
        {
            var lastTwoDigits = cardId % 100;
            return lastTwoDigits >= 80 && lastTwoDigits <= 99;
        }
    }
}
