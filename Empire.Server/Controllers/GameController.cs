using Empire.Server.Services;
using Empire.Shared.Models.DTOs;
using Empire.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Collections.Generic;
using Empire.Shared.Models;
using MongoDB.Driver;
using Empire.Server.Interfaces;
using Empire.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Empire.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class GameController : ControllerBase
    {
        private readonly IHubContext<GameHub> _hub;
        private readonly GameSessionService _sessionService;
        private readonly GameStateService _gameStateService;
        private readonly DeckService _deckService;
        private readonly ICardService _cardService;
        private readonly IMongoCollection<PlayerDeck> _deckCollection;

        public GameController(
            GameSessionService sessionService,
            GameStateService gameStateService,
            DeckService deckService,
            ICardService cardService,
            IMongoDbService mongo,
            IHubContext<GameHub> hub) // 🧩 Add this
        {
            _sessionService = sessionService;
            _gameStateService = gameStateService;
            _deckService = deckService;
            _cardService = cardService;
            _deckCollection = mongo.DeckDatabase.GetCollection<PlayerDeck>("PlayerDecks");
            _hub = hub;
        }

        [HttpGet("{gameId}/state")]
        public async Task<IActionResult> GetGameState(string gameId)
        {
            var state = await _sessionService.GetGameState(gameId);
            if (state == null)
                return NotFound("Game not found.");

            return Ok(state);
        }
        [HttpPost("reorder")]
        public async Task<IActionResult> UpdateBoardOrder([FromBody] BoardPositionUpdate update)
        {
            if (string.IsNullOrEmpty(update.GameId) || string.IsNullOrEmpty(update.PlayerId))
                return BadRequest("Missing game or player ID.");

            var game = await _sessionService.GetGameState(update.GameId);
            if (game == null)
                return NotFound("Game not found.");

            game.PlayerHands[update.PlayerId] = update.NewOrder;
            await _sessionService.SaveGameState(update.GameId, game);

            // 🔔 Notify everyone else in the group
            await _hub.Clients.Group(update.GameId)
                .SendAsync("ReceiveBoardUpdate", update);

            return Ok();
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

            var deck = await _deckCollection.Find(d => d.Id == request.DeckId).FirstOrDefaultAsync();
            if (deck == null || (!deck.CivicDeck.Any() && !deck.MilitaryDeck.Any()))
                return BadRequest("Invalid or missing deck.");

            var fullCivicDeck = (await _cardService.GetDeckCards(deck.CivicDeck))
                .Select(c => { c.DeckType = "Civic"; return c; }).ToList();

            var fullMilitaryDeck = (await _cardService.GetDeckCards(deck.MilitaryDeck))
                .Select(c => { c.DeckType = "Military"; return c; }).ToList();

            var fullDeck = fullCivicDeck.Concat(fullMilitaryDeck).ToList();

            var rawDeck = fullDeck
                .GroupBy(card => card.CardId)
                .Select(g => new RawDeckEntry
                {
                    CardId = g.Key,
                    Count = g.Count(),
                    DeckType = g.First().DeckType
                }).ToList();

            var gameId = await _sessionService.CreateGameSession(request.Player1, rawDeck);
            return Ok(gameId);
        }

        [HttpPost("{gameId}/draw/{playerId}/{type}")]
        public async Task<IActionResult> DrawCard(string gameId, string playerId, string type)
        {
            var gameState = await _sessionService.GetGameState(gameId);
            if (gameState == null)
                return NotFound("Game not found.");

            if (!gameState.PlayerDecks.TryGetValue(playerId, out var deck) || deck.Count == 0)
                return BadRequest("Deck is empty or missing.");

            var drawFrom = type.ToLower() switch
            {
                "civic" => deck.Where(c => c.DeckType == "Civic").ToList(),
                "military" => deck.Where(c => c.DeckType == "Military").ToList(),
                _ => null
            };

            if (drawFrom == null || drawFrom.Count == 0)
                return BadRequest($"No {type} cards left in deck.");

            var random = new Random();
            var drawn = drawFrom[random.Next(drawFrom.Count)];
            var indexToRemove = deck.FindIndex(c => c.CardId == drawn.CardId);
            if (indexToRemove >= 0)
                deck.RemoveAt(indexToRemove);

            if (!gameState.PlayerHands.ContainsKey(playerId))
                gameState.PlayerHands[playerId] = new List<int>();

            gameState.PlayerHands[playerId].Add(drawn.CardId);

            await _sessionService.SaveGameState(gameId, gameState);

            // 🧠 🔔 Broadcast to others
            await _hub.Clients.Group(gameId).SendAsync("ReceiveBoardUpdate", new BoardPositionUpdate
            {
                GameId = gameId,
                PlayerId = playerId,
                NewOrder = gameState.PlayerHands[playerId]
            });

            return Ok(drawn.CardId);
        }



        private bool IsCivic(string? type)
        {
            var t = type?.Trim().ToLowerInvariant();
            return t == "villager" || t == "settlement";
        }

        [HttpPost("join/{gameId}/{playerId}")]
        public async Task<IActionResult> JoinGame(string gameId, string playerId)
        {
            var deck = await _deckService.GetDeckAsync(playerId);
            if (deck == null || (!deck.CivicDeck.Any() && !deck.MilitaryDeck.Any()))
                return BadRequest("No deck found for this player.");

            var existingState = await _sessionService.GetGameState(gameId);
            if (existingState == null)
                return NotFound("Game not found.");

            var fullCivicDeck = await _cardService.GetDeckCards(deck.CivicDeck);
            var fullMilitaryDeck = await _cardService.GetDeckCards(deck.MilitaryDeck);
            var combinedDeck = fullCivicDeck.Concat(fullMilitaryDeck).ToList();

            // ✅ Just let _sessionService handle the full card join logic
            var success = await _sessionService.JoinGame(gameId, playerId, combinedDeck);

            if (!success)
                return BadRequest("Could not join game. It may already have two players.");

            return Ok(gameId);
        }

    }

}