using Empire.Server.Services;
using Empire.Shared.Models.DTOs;
using Empire.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Collections.Generic;
using Empire.Shared.Models;

namespace Empire.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly GameSessionService _sessionService;
        private readonly GameStateService _gameStateService;
        private readonly DeckService _deckService;
        private readonly ICardService _cardService;

        public GameController(
            GameSessionService sessionService,
            GameStateService gameStateService,
            DeckService deckService,
            ICardService cardService)
        {
            _sessionService = sessionService;
            _gameStateService = gameStateService;
            _deckService = deckService;
            _cardService = cardService;
        }
        [HttpGet("state/{gameId}")]
        public async Task<IActionResult> GetGameState(string gameId)
        {
            var state = await _sessionService.GetGameState(gameId);
            if (state == null)
                return NotFound("Game not found.");

            return Ok(state);
        }



        [HttpGet("open")]
        public async Task<ActionResult<List<GamePreview>>> GetOpenGames()
        {
            var games = await _sessionService.ListOpenGames();
            var previews = games.Select(g => new GamePreview
            {
                GameId = g.GameId,
                HostPlayer = g.Player1,
                IsJoinable = string.IsNullOrEmpty(g.Player2)
            }).ToList();

            return Ok(previews);
        }

        [HttpPost("create")]
        public async Task<ActionResult<string>> CreateGame([FromBody] GameStartRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Player1))
                return BadRequest("Player1 is required.");

            var deck = await _deckService.GetDeckAsync(request.DeckName);
            if (deck == null || (!deck.CivicDeck.Any() && !deck.MilitaryDeck.Any()))
                return BadRequest("No deck found with that name.");

            var fullCivicDeck = await _cardService.GetDeckCards(deck.CivicDeck);
            var fullMilitaryDeck = await _cardService.GetDeckCards(deck.MilitaryDeck);
            var fullDeck = fullCivicDeck.Concat(fullMilitaryDeck).ToList();

            var rawDeck = fullDeck
                .GroupBy(card => card.CardId)
                .Select(g => new RawDeckEntry
                {
                    CardId = g.Key,
                    Count = g.Count(),
                    DeckType = InferDeckType(g.First())
                }).ToList();

            var gameId = await _sessionService.CreateGameSession(request.Player1, rawDeck);

            return Ok(gameId);
        }

        private string InferDeckType(Card card)
        {
            return card.Type.ToLower() switch
            {
                "villager" => "Civic",
                "settlement" => "Civic",
                _ => "Military"
            };
        }

        [HttpPost("join/{gameId}/{playerId}")]
        public async Task<IActionResult> JoinGame(string gameId, string playerId)
        {
            var deck = await _deckService.GetDeckAsync(playerId);

            if (deck == null || deck.CivicDeck.Count == 0 && deck.MilitaryDeck.Count == 0)
                return BadRequest("No deck found for this player.");

            var existingState = await _sessionService.GetGameState(gameId);
            if (existingState == null)
                return NotFound("Game not found.");

            // ✅ Pull the full cards for each half of the deck
            var fullCivicDeck = await _cardService.GetDeckCards(deck.CivicDeck);
            var fullMilitaryDeck = await _cardService.GetDeckCards(deck.MilitaryDeck);

            // ✅ Combine both civic and military decks into the game state
            //deck.CivicDeck.AddRange(fullCivicDeck);
            //deck.MilitaryDeck.AddRange(fullMilitaryDeck);

            var combinedDeck = fullCivicDeck.Concat(fullMilitaryDeck).ToList();

            // ✅ Initialize game state with raw IDs
            var civicCardIds = fullCivicDeck.Select(card => card.CardId).ToList();
            var militaryCardIds = fullMilitaryDeck.Select(card => card.CardId).ToList();
            _gameStateService.InitializeGame(playerId, civicCardIds, militaryCardIds);

            // ✅ Join game session
            //  The JoinGame method expects a List<int> but we are passing a List<Card>
            //  We need to extract the card IDs from the combinedDeck
            // var combinedCardIds = combinedDeck.Select(card => card.CardId).ToList();
            await _sessionService.JoinGame(gameId, playerId, combinedDeck);

            return Ok(gameId);
        }
    }
}