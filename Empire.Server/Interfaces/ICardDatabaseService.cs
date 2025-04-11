public interface ICardDatabaseService
{
    IEnumerable<CardData> GetAllCards();
    CardData? GetCardById(int id); //
}