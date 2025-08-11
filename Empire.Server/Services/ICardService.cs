using Empire.Shared.Models;

namespace Empire.Server.Services
{
    public interface ICardService
    {
        Task<List<Card>> GetDeckCards(List<int> cardIds);
        IReadOnlyList<Card> GetHand();
        IReadOnlyList<Card> GetBoard();
        IReadOnlyList<Card> GetGraveyard();
        IReadOnlyList<Card> GetSealedAway();

        Card? DrawCard();
        bool PlayCard(Card card);
        bool MoveToGraveyard(Card card);
        bool SealCard(Card card);
        void ShuffleDeck();
        void PutOnTopOfDeck(Card card);
        void PutOnBottomOfDeck(Card card);
        IReadOnlyList<Card> PeekTopOfDeck(int count, bool draw = false);
    }
}
