using Empire.Shared.Models;
using Empire.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Empire.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeckController : ControllerBase
    {
        private readonly DeckService _deckService;

        public DeckController(DeckService deckService)
        {
            _deckService = deckService;
        }

        [HttpGet("prelobby/decks")]
        public async Task<List<string>> GetUploadedDeckNames()
        {
            var allDecks = await _deckService.GetAllDecksAsync();
            return allDecks.Select(d => d.DeckName).Distinct().ToList();
        }
    }
}
