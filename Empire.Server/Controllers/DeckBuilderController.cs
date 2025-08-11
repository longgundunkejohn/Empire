using Microsoft.AspNetCore.Mvc;
using Empire.Shared.Models;
using Empire.Shared.Models.DTOs;
using Empire.Server.Services;

namespace Empire.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeckBuilderController : ControllerBase
    {
        private readonly ICardDatabaseService _cardDatabase;
        private readonly DeckLoaderService _deckLoader;
        private readonly ILogger<DeckBuilderController> _logger;

        public DeckBuilderController(
            ICardDatabaseService cardDatabase,
            DeckLoaderService deckLoader,
            ILogger<DeckBuilderController> logger)
        {
            _cardDatabase = cardDatabase;
            _deckLoader = deckLoader;
            _logger = logger;
        }

        [HttpGet("cards")]
        public ActionResult<IEnumerable<CardData>> GetAllCards()
        {
            try
            {
                var cards = _cardDatabase.GetAllCards();
                _logger.LogInformation("📋 Retrieved {Count} cards for deck builder", cards.Count());
                return Ok(cards);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving cards for deck builder");
                return StatusCode(500, "Error retrieving cards");
            }
        }

        [HttpPost("save")]
        public ActionResult SaveDeck([FromBody] PlayerDeck playerDeck)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(playerDeck.PlayerName))
                {
                    return BadRequest("Player name is required");
                }

                // Convert PlayerDeck to RawDeckEntry format
                var rawDeckEntries = new List<RawDeckEntry>();

                // Add civic cards
                foreach (var cardId in playerDeck.CivicDeck)
                {
                    var existingEntry = rawDeckEntries.FirstOrDefault(e => e.CardId == cardId);
                    if (existingEntry != null)
                    {
                        existingEntry.Count++;
                    }
                    else
                    {
                        rawDeckEntries.Add(new RawDeckEntry
                        {
                            CardId = cardId,
                            Count = 1,
                            DeckType = "civic",
                            Player = playerDeck.PlayerName
                        });
                    }
                }

                // Add military cards
                foreach (var cardId in playerDeck.MilitaryDeck)
                {
                    var existingEntry = rawDeckEntries.FirstOrDefault(e => e.CardId == cardId);
                    if (existingEntry != null)
                    {
                        existingEntry.Count++;
                    }
                    else
                    {
                        rawDeckEntries.Add(new RawDeckEntry
                        {
                            CardId = cardId,
                            Count = 1,
                            DeckType = "military",
                            Player = playerDeck.PlayerName
                        });
                    }
                }

                // Save to database
                _deckLoader.SaveDeckToDatabase(playerDeck.PlayerName, rawDeckEntries);

                _logger.LogInformation("✅ Saved deck for player {Player} with {Count} unique cards", 
                    playerDeck.PlayerName, rawDeckEntries.Count);

                return Ok(new { message = "Deck saved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error saving deck for player {Player}", playerDeck?.PlayerName);
                return StatusCode(500, "Error saving deck");
            }
        }

        [HttpGet("player/{playerName}")]
        public ActionResult<PlayerDeck> GetPlayerDeck(string playerName)
        {
            try
            {
                var playerDeck = _deckLoader.LoadDeck(playerName);
                
                if (playerDeck.CivicDeck.Count == 0 && playerDeck.MilitaryDeck.Count == 0)
                {
                    return NotFound($"No deck found for player {playerName}");
                }

                _logger.LogInformation("📋 Retrieved deck for player {Player}", playerName);
                return Ok(playerDeck);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving deck for player {Player}", playerName);
                return StatusCode(500, "Error retrieving deck");
            }
        }

        [HttpPost("populate-sample-cards")]
        public ActionResult PopulateSampleCards()
        {
            try
            {
                // This endpoint can be used to populate the database with sample cards
                // if the MongoDB collection is empty
                var existingCards = _cardDatabase.GetAllCards();
                
                if (existingCards.Any())
                {
                    return Ok(new { message = $"Database already contains {existingCards.Count()} cards" });
                }

                // Generate sample cards (you can expand this list)
                var sampleCards = GenerateSampleCardData();
                
                // Note: You'll need to implement a method to save cards to the database
                // This is just a placeholder for now
                _logger.LogInformation("🎯 Would populate {Count} sample cards", sampleCards.Count);
                
                return Ok(new { message = $"Would populate {sampleCards.Count} sample cards" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error populating sample cards");
                return StatusCode(500, "Error populating sample cards");
            }
        }

        private List<CardData> GenerateSampleCardData()
        {
            var cards = new List<CardData>();
            
            // Sample Army Cards (1001-1079)
            cards.Add(new CardData { CardID = 1001, Name = "Conscript", CardType = "Unit", Faction = "Amali", Cost = 1, CardText = "Basic infantry unit", ImageFileName = "images/Cards/1001.jpg" });
            cards.Add(new CardData { CardID = 1002, Name = "Knight of Songdu", CardType = "Unit", Faction = "Amali", Cost = 3, CardText = "Elite cavalry unit", ImageFileName = "images/Cards/1002.jpg" });
            cards.Add(new CardData { CardID = 1003, Name = "Amali Archer", CardType = "Unit", Faction = "Amali", Cost = 2, CardText = "Ranged unit", ImageFileName = "images/Cards/1003.jpg" });
            cards.Add(new CardData { CardID = 1004, Name = "Battle Charge", CardType = "Tactic", Faction = "Amali", Cost = 2, CardText = "Give a unit +2 attack this turn", ImageFileName = "images/Cards/1004.jpg" });
            cards.Add(new CardData { CardID = 1005, Name = "Kyrushima Samurai", CardType = "Unit", Faction = "Kyrushima", Cost = 4, CardText = "Honor-bound warrior", ImageFileName = "images/Cards/1005.jpg" });
            cards.Add(new CardData { CardID = 1006, Name = "Hjordict Berserker", CardType = "Unit", Faction = "Hjordict", Cost = 3, CardText = "Fierce northern warrior", ImageFileName = "images/Cards/1006.jpg" });
            cards.Add(new CardData { CardID = 1007, Name = "Ndembe Spearman", CardType = "Unit", Faction = "Ndembe", Cost = 2, CardText = "Tribal warrior", ImageFileName = "images/Cards/1007.jpg" });
            cards.Add(new CardData { CardID = 1008, Name = "Ohotec Scout", CardType = "Unit", Faction = "Ohotec", Cost = 1, CardText = "Fast reconnaissance unit", ImageFileName = "images/Cards/1008.jpg" });
            cards.Add(new CardData { CardID = 1009, Name = "Neutral Mercenary", CardType = "Unit", Faction = "Neutral", Cost = 3, CardText = "Hired sword", ImageFileName = "images/Cards/1009.jpg" });
            cards.Add(new CardData { CardID = 1010, Name = "War Banner", CardType = "Tactic", Faction = "Neutral", Cost = 1, CardText = "Boost nearby units", ImageFileName = "images/Cards/1010.jpg" });

            // Sample Civic Cards (1080-1099)
            cards.Add(new CardData { CardID = 1080, Name = "Consecrated Paladin", CardType = "Settlement", Faction = "Amali", Cost = 3, CardText = "Holy warrior settlement", ImageFileName = "images/Cards/1080.jpg" });
            cards.Add(new CardData { CardID = 1081, Name = "High Priestess Stella", CardType = "Villager", Faction = "Amali", Cost = 4, CardText = "Religious leader", ImageFileName = "images/Cards/1081.jpg" });
            cards.Add(new CardData { CardID = 1082, Name = "Market Square", CardType = "Settlement", Faction = "Neutral", Cost = 2, CardText = "Trade hub", ImageFileName = "images/Cards/1082.jpg" });
            cards.Add(new CardData { CardID = 1083, Name = "Temple of Light", CardType = "Settlement", Faction = "Amali", Cost = 3, CardText = "Sacred building", ImageFileName = "images/Cards/1083.jpg" });
            cards.Add(new CardData { CardID = 1084, Name = "Wise Elder", CardType = "Villager", Faction = "Neutral", Cost = 2, CardText = "Provides counsel", ImageFileName = "images/Cards/1084.jpg" });
            cards.Add(new CardData { CardID = 1085, Name = "Blacksmith", CardType = "Villager", Faction = "Neutral", Cost = 2, CardText = "Crafts weapons", ImageFileName = "images/Cards/1085.jpg" });
            cards.Add(new CardData { CardID = 1086, Name = "Monastery", CardType = "Settlement", Faction = "Kyrushima", Cost = 4, CardText = "Center of learning", ImageFileName = "images/Cards/1086.jpg" });
            cards.Add(new CardData { CardID = 1087, Name = "Farmer", CardType = "Villager", Faction = "Neutral", Cost = 1, CardText = "Provides food", ImageFileName = "images/Cards/1087.jpg" });

            return cards;
        }
    }
}
