using Empire.Shared.Models;
using Empire.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Empire.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeckController : ControllerBase
    {
        private readonly CardFactory _cardFactory;

        public DeckController(CardFactory cardFactory)
        {
            _cardFactory = cardFactory;
        }

        [HttpGet]
        public async Task<ActionResult<List<Card>>> GetTestDeck()
        {
            var testDeckList = new List<(int CardId, int Count)>
            {
                (101, 3),
                (102, 2)
            };

            var cards = await _cardFactory.CreateDeckAsync(testDeckList);
            return Ok(cards);
        }
    }
}

