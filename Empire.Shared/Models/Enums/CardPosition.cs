using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Empire.Shared.Models
{
    public enum CardPosition
    {
        ArmyHand,      // In player's army hand
        CivicHand,     // In player's civic hand
        Heartland,     // Safe zone for units
        Advancing,     // Unit attacking a territory
        Occupying,     // Unit controlling a territory
        Villager,      // Civic card played as villager in heartland
        Settlement,    // Civic card used to settle a territory
        Graveyard,     // Dead units or used tactics
        ArmyDeck,      // In army deck
        CivicDeck      // In civic deck
    }
}
