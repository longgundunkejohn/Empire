using Empire.Shared.Models;
using Empire.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Empire.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeckController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly DeckService _deckService;
        private readonly ICardDatabaseService _cardDatabase;
        private readonly ILogger<DeckController> _logger;

        public DeckController(
            UserService userService, 
            DeckService deckService,
            ICardDatabaseService cardDatabase,
            ILogger<DeckController> logger)
        {
            _userService = userService;
            _deckService = deckService;
            _cardDatabase = cardDatabase;
            _logger = logger;
        }

        [HttpGet("prelobby/decks")]
        public async Task<List<string>> GetUploadedDeckNames()
        {
            var allDecks = await _deckService.GetAllDecksAsync();
            return allDecks.Select(d => d.DeckName).Distinct().ToList();
        }

        [HttpGet("user-decks")]
        public async Task<ActionResult> GetUserDecks()
        {
            try
            {
                var username = GetCurrentUsername();
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                var decks = await _userService.GetUserDecksAsync(username);
                return Ok(decks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user decks");
                return StatusCode(500, new { message = "Failed to get user decks" });
            }
        }

        [HttpPost("save")]
        public async Task<ActionResult> SaveDeck([FromBody] SaveDeckRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var username = GetCurrentUsername();
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                // Validate deck structure for Empire TCG
                var validationErrors = ValidateEmpireDeck(request.ArmyCards, request.CivicCards);
                if (validationErrors.Any())
                    return BadRequest(new { message = "Invalid deck", errors = validationErrors });

                var deck = await _userService.SaveDeckAsync(username, request.Name, request.ArmyCards, request.CivicCards);
                
                _logger.LogInformation("User {Username} saved deck '{DeckName}' with {ArmyCount} army and {CivicCount} civic cards",
                    username, request.Name, request.ArmyCards.Count, request.CivicCards.Count);

                return Ok(new { message = "Deck saved successfully", deckId = deck.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving deck");
                return StatusCode(500, new { message = "Failed to save deck" });
            }
        }

        [HttpDelete("{deckId}")]
        public async Task<ActionResult> DeleteDeck(int deckId)
        {
            try
            {
                var username = GetCurrentUsername();
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                // Verify the deck belongs to the current user
                var userDecks = await _userService.GetUserDecksAsync(username);
                var deck = userDecks.FirstOrDefault(d => d.Id == deckId);
                
                if (deck == null)
                    return NotFound(new { message = "Deck not found" });

                var success = await _userService.DeleteDeckAsync(username, deck.Name);
                
                if (success)
                {
                    _logger.LogInformation("User {Username} deleted deck {DeckId}", username, deckId);
                    return Ok(new { message = "Deck deleted successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Failed to delete deck" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting deck {DeckId}", deckId);
                return StatusCode(500, new { message = "Failed to delete deck" });
            }
        }

        private List<string> ValidateEmpireDeck(List<int> armyCards, List<int> civicCards)
        {
            var errors = new List<string>();

            var totalCards = armyCards.Count + civicCards.Count;

            // Empire TCG deck rules
            if (totalCards < 45)
                errors.Add("Deck must have at least 45 cards");
            if (totalCards > 60)
                errors.Add("Deck cannot have more than 60 cards");
            if (armyCards.Count < 30)
                errors.Add("Must have at least 30 Army cards");
            if (civicCards.Count < 15)
                errors.Add("Must have at least 15 Civic cards");

            // Check for too many copies of individual cards
            var allCards = armyCards.Concat(civicCards);
            var cardCounts = allCards.GroupBy(id => id).Where(g => g.Count() > 3);
            foreach (var cardGroup in cardCounts)
            {
                errors.Add($"Card {cardGroup.Key} appears {cardGroup.Count()} times (max 3 allowed)");
            }

            return errors;
        }

        private string GetCurrentUsername()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }

    public class SaveDeckRequest
    {
        public string Name { get; set; } = string.Empty;
        public List<int> ArmyCards { get; set; } = new();
        public List<int> CivicCards { get; set; } = new();
    }

    public class ValidateDeckRequest
    {
        public string DeckName { get; set; } = string.Empty;
    }
}
