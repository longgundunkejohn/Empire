using Empire.Shared.Models;

namespace Empire.Client.Services
{
    public class MockDeckService
    {
        private static readonly List<PlayerDeck> _sampleDecks = new()
        {
            new PlayerDeck
            {
                Id = "amali-starter",
                PlayerName = "TestPlayer",
                DeckName = "Amali Starter",
                CivicDeck = MockCardDataService.GetSampleAmaliCivicDeck(),
                MilitaryDeck = MockCardDataService.GetSampleAmaliMilitaryDeck()
            },
            new PlayerDeck
            {
                Id = "horudjet-starter",
                PlayerName = "TestPlayer",
                DeckName = "Horudjet Starter",
                CivicDeck = MockCardDataService.GetSampleHorudjetCivicDeck(),
                MilitaryDeck = MockCardDataService.GetSampleHorudjetMilitaryDeck()
            },
            new PlayerDeck
            {
                Id = "amali-advanced",
                PlayerName = "TestPlayer",
                DeckName = "Amali Advanced",
                CivicDeck = new List<int> { 60, 61, 62, 63, 64, 65, 66, 48, 48, 48, 48, 48, 48, 48, 48 },
                MilitaryDeck = new List<int> 
                { 
                    // Elite Amali deck with higher tier cards
                    3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
                    32, 33, 34, 35, 40, 41, 43, 44, 49, 50,
                    15, 16, 17, 18, 19, 20, 21, 22, 23, 24
                }
            }
        };

        public static List<PlayerDeck> GetDecksForPlayer(string playerName)
        {
            return _sampleDecks.Select(deck => new PlayerDeck
            {
                Id = deck.Id,
                PlayerName = playerName,
                DeckName = deck.DeckName,
                CivicDeck = deck.CivicDeck,
                MilitaryDeck = deck.MilitaryDeck
            }).ToList();
        }

        public static PlayerDeck? GetDeckById(string deckId)
        {
            return _sampleDecks.FirstOrDefault(d => d.Id == deckId);
        }

        public static PlayerDeck? GetDeckByName(string playerName, string deckName)
        {
            return _sampleDecks.FirstOrDefault(d => d.DeckName == deckName);
        }

        // Create sample decks for different factions
        public static PlayerDeck CreateAmaliDeck(string playerName) => new()
        {
            Id = $"amali-{Guid.NewGuid()}",
            PlayerName = playerName,
            DeckName = "Amali Empire",
            CivicDeck = MockCardDataService.GetSampleAmaliCivicDeck(),
            MilitaryDeck = MockCardDataService.GetSampleAmaliMilitaryDeck()
        };

        public static PlayerDeck CreateHorudjetDeck(string playerName) => new()
        {
            Id = $"horudjet-{Guid.NewGuid()}",
            PlayerName = playerName,
            DeckName = "Horudjet Dynasty",
            CivicDeck = MockCardDataService.GetSampleHorudjetCivicDeck(),
            MilitaryDeck = MockCardDataService.GetSampleHorudjetMilitaryDeck()
        };

        public static PlayerDeck CreateRandomDeck(string playerName, string deckName)
        {
            var random = new Random();
            var allCards = MockCardDataService.GetAllCards();
            
            var civicCards = MockCardDataService.GetCivicCards();
            var militaryCards = MockCardDataService.GetMilitaryCards();

            // Create random civic deck (15 cards)
            var civicDeck = new List<int>();
            for (int i = 0; i < 15; i++)
            {
                var randomCard = civicCards[random.Next(civicCards.Count)];
                civicDeck.Add(randomCard.CardId);
            }

            // Create random military deck (30 cards)
            var militaryDeck = new List<int>();
            for (int i = 0; i < 30; i++)
            {
                var randomCard = militaryCards[random.Next(militaryCards.Count)];
                militaryDeck.Add(randomCard.CardId);
            }

            return new PlayerDeck
            {
                Id = $"random-{Guid.NewGuid()}",
                PlayerName = playerName,
                DeckName = deckName,
                CivicDeck = civicDeck,
                MilitaryDeck = militaryDeck
            };
        }
    }
}
