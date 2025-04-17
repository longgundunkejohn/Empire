using Empire.Shared.Models;

public interface ICardDatabaseService
{
    IEnumerable<CardData> GetAllCards();
    CardData? GetCardById(int id);

    Task<List<Card>> GetDeckCards(List<int> cardIds); // ⬅️ Add this line
}
