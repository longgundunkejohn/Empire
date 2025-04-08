using Empire.Server.Interfaces;
using Empire.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Empire.Server.Services
{
    public class CardFactory
    {
        private readonly ICardDatabaseService _cardDb;

        public CardFactory(ICardDatabaseService cardDb)
        {
            _cardDb = cardDb;
        }

        public Task<Card?> CreateCardFromIdAsync(int id)
        {
            var data = _cardDb.GetAllCards().FirstOrDefault(c => c.CardID == id);
            return Task.FromResult(data != null ? new Card(data) : null);
        }

        public Task<List<Card>> CreateDeckAsync(List<(int CardId, int Count)> deckList)
        {
            var cardDataList = _cardDb.GetAllCards();

            // Group by CardID and take the first for each to avoid duplicate key issues
            var cardLookup = cardDataList
                .GroupBy(c => c.CardID)
                .ToDictionary(g => g.Key, g => g.First());

            var result = new List<Card>();

            foreach (var (id, count) in deckList)
            {
                if (cardLookup.TryGetValue(id, out var data))
                {
                    for (int i = 0; i < count; i++)
                        result.Add(new Card(data));
                }
            }

            return Task.FromResult(result);
        }
    }
}