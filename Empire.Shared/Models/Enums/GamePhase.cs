using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Empire.Shared.Models
{
    public enum GamePhase
    {
        Strategy,      // Deploy cards, play villagers, settle territories, commit units
        Battle,        // Maneuvers step + Combat step
        Replenishment  // Unexert units, draw cards, pass initiative tracker
    }
}
