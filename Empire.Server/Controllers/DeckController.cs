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

    }
}

