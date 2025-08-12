using Empire.Shared.Models;

namespace Empire.Server.Services
{
    public interface ICardDatabaseService
    {
        IEnumerable<CardData> GetAllCards();
        CardData? GetCardById(int id);
        Task<List<Card>> GetDeckCards(List<int> cardIds);
        List<CardData> GetCardsByType(string cardType);
        List<CardData> GetCardsByFaction(string faction);
        List<CardData> GetCardsByTier(string tier);
        List<CardData> SearchCards(string searchTerm);
    }
}
