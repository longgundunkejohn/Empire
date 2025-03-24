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

        // Creates one Card from ID
        public async Task<Card?> CreateCardFromIdAsync(int id)
        {
            var data = await _cardCollection.Find(c => c.CardId == id).FirstOrDefaultAsync();
            return data != null ? new Card(data) : null;
        }

        // Creates a full deck from a list of (CardId, Count)
        public async Task<List<Card>> CreateDeckAsync(List<(int CardId, int Count)> deckList)
        {
            var cardIds = deckList.Select(d => d.CardId).Distinct().ToList();

            var filter = Builders<CardData>.Filter.In(c => c.CardId, cardIds);
            var allCardData = await _cardCollection.Find(filter).ToListAsync();

            var cardDictionary = allCardData.ToDictionary(c => c.CardId);

            var cards = new List<Card>();
            foreach (var (id, count) in deckList)
            {
                if (cardDictionary.TryGetValue(id, out var data))
                {
                    for (int i = 0; i < count; i++)
                    {
                        cards.Add(new Card(data));
                    }
                }
                else
                {
                    // Optional: log or throw if CardId is missing in DB
                }
            }

            return cards;
        }
    }
}
