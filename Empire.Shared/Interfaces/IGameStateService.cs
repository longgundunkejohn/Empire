using Empire.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Empire.Shared.Interfaces
{
    public interface IGameStateService
    {

        List<Card> GetPlayerHand();
        List<Card> GetBoard();
        void MoveCardToBoard(Card card);
        void MoveCardToGraveyard(Card card);
    }

}
