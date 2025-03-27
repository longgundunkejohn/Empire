using Empire.Shared.Models;

public interface ICardDatabaseService
{
    IEnumerable<CardData> GetAllCards();
    CardData? GetCardById(string id);
}
