using Empire.Shared.Models;
using MongoDB.Driver;

namespace Empire.Server.Services
{
    public class CardFactory
    {
        private readonly IMongoCollection<CardData> _cardCollection;

        public CardFactory(IMongoDatabase database)
        {
            _cardCollection = database.GetCollection<CardData>("Cards");
        }

        // Create a single Card from its ID
        public async Task<Card?> CreateCardFromIdAsync(int id)
        {
            var data = await _cardCollection.Find(c => c.CardID == id).FirstOrDefaultAsync();
            return data != null ? new Card(data) : null;
        }

        // Create a full deck from a list of (CardId, Count)
        public async Task<List<Card>> CreateDeckAsync(List<(int CardId, int Count)> deckList)
        {
            var ids = deckList.Select(d => d.CardId).Distinct().ToList();
            var filter = Builders<CardData>.Filter.In(c => c.CardID, ids);
            var cardDataList = await _cardCollection.Find(filter).ToListAsync();
            var cardLookup = cardDataList.ToDictionary(c => c.CardID);

            var result = new List<Card>();
            foreach (var (id, count) in deckList)
            {
                if (cardLookup.TryGetValue(id, out var data))
                {
                    for (int i = 0; i < count; i++)
                        result.Add(new Card(data));
                }
            }

            return result;
        }
    }
}
