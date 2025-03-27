using Empire.Shared.Models;
using Empire.Server.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Empire.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CardController : ControllerBase
    {
        private readonly ICardDatabaseService _cardDb;

        public CardController(ICardDatabaseService cardDb)
        {
            _cardDb = cardDb;
        }

        [HttpGet]
        public ActionResult<IEnumerable<CardData>> GetAll()
        {
            return Ok(_cardDb.GetAllCards());
        }

        [HttpGet("{id}")]
        public ActionResult<CardData> Get(string id)
        {
            var card = _cardDb.GetCardById(id);
            if (card == null) return NotFound();
            return Ok(card);
        }
    }
}
