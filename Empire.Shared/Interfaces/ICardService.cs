using Empire.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Empire.Shared.Interfaces
{
    public interface ICardService
    {
        List<Card> GetDeck();
        List<Card> GetHand();
        List<Card> GetGraveyard();
        List<Card> GetBoard();
        void ShuffleDeck();
        Card? DrawCard();
        void PlayCard(Card card);
        void MoveToGraveyard(Card card);
    }

}
