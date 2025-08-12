using Empire.Shared.Models;

namespace Empire.Server.Services
{
    public class CardFactory
    {
        private readonly ICardDatabaseService _cardDatabaseService;

        public CardFactory(ICardDatabaseService cardDatabaseService)
        {
            _cardDatabaseService = cardDatabaseService;
        }

        public Card? CreateCard(int cardId)
        {
            var cardData = _cardDatabaseService.GetCardById(cardId);
            if (cardData == null)
                return null;

            return new Card
            {
                CardId = cardData.CardID,
                Name = cardData.Name,
                CardText = cardData.CardText,
                Faction = cardData.Faction,
                Type = cardData.CardType,
                ImagePath = $"images/Cards/{cardData.CardID}.jpg",
                IsExerted = false,
                CurrentDamage = 0,
                Cost = cardData.Cost,
                Attack = cardData.Attack,
                Defense = cardData.Defence,
                Tier = cardData.Tier,
                IsUnique = !string.IsNullOrEmpty(cardData.Unique) && cardData.Unique.ToLower() == "true"
            };
        }

        public List<Card> CreateCards(List<int> cardIds)
        {
            var cards = new List<Card>();
            foreach (var cardId in cardIds)
            {
                var card = CreateCard(cardId);
                if (card != null)
                {
                    cards.Add(card);
                }
            }
            return cards;
        }
    }
}
