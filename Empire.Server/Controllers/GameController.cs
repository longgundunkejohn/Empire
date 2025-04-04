using Empire.Server.Services;
using Empire.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Empire.Shared.DTOs;
using Empire.Shared.Models.DTOs;
using CsvHelper;
using System.Globalization;
using Empire.Server.Parsing;
namespace Empire.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly ICardDatabaseService _cardDatabase;
        private readonly ICardService _cardService;
        private readonly GameSessionService _sessionService;
        private readonly CardFactory _cardFactory;
        private readonly DeckLoaderService _deckLoader;

        public GameController(
            GameSessionService sessionService,
            CardFactory cardFactory,
            DeckLoaderService deckLoader,
            ICardService cardService,
            ICardDatabaseService cardDatabase) // 🔥 add this
        {
            _sessionService = sessionService;
            _cardFactory = cardFactory;
            _deckLoader = deckLoader;
            _cardService = cardService;

            // if you also need to store/use _cardDatabase later
            _cardDatabase = cardDatabase;
        }

        [HttpGet("cards/{gameId}/{playerId}")]
        public async Task<ActionResult<List<Card>>> GetPlayerCards(string gameId, string playerId)
        {
            var game = await _sessionService.GetGameState(gameId);
            if (game == null) return NotFound("Game not found.");

            var cardIds = new HashSet<int>();

            if (game.PlayerHands.TryGetValue(playerId, out var hand))
                cardIds.UnionWith(hand);

            if (game.PlayerBoard.TryGetValue(playerId, out var board))
                cardIds.UnionWith(board.Select(bc => bc.CardId));

            if (game.PlayerDecks.TryGetValue(playerId, out var deck))
            {
                cardIds.UnionWith(deck.CivicDeck);
                cardIds.UnionWith(deck.MilitaryDeck);
            }

            var cards = await _cardService.GetDeckCards(cardIds.ToList());
            return Ok(cards);
        }

        [HttpGet("open")]
        public async Task<ActionResult<List<GamePreview>>> GetOpenGames()
        {
            var openGames = await _sessionService.ListOpenGames();

            Console.WriteLine($"[GetOpenGames] Found {openGames?.Count ?? 0} open games.");

            if (openGames == null)
            {
                Console.WriteLine("[GetOpenGames] openGames is null");
                return Problem("Failed to retrieve open games.");
            }

            var previews = openGames.Select(g => new GamePreview
            {
                GameId = g.GameId,
                HostPlayer = g.Player1,
                IsJoinable = string.IsNullOrEmpty(g.Player2)
            }).ToList();

            return Ok(previews);
        }


        [HttpGet("deck/{gameId}/{playerId}")]
        public async Task<ActionResult<List<Card>>> GetPlayerDeck(string gameId, string playerId)
        {
            var state = await _sessionService.GetGameState(gameId);
            if (state == null) return NotFound("Game not found.");
            if (!state.PlayerDecks.ContainsKey(playerId)) return NotFound("Player not found in game.");

            var deckList = state.PlayerDecks[playerId].CivicDeck
                .Concat(state.PlayerDecks[playerId].MilitaryDeck)
                .GroupBy(id => id)
                .Select(g => (CardId: g.Key, Count: g.Count()))
                .ToList();

            var fullDeck = await _cardFactory.CreateDeckAsync(deckList);
            return Ok(fullDeck);
        }

        [HttpGet("state/{gameId}/{playerId}")]
        public async Task<ActionResult<GameState>> GetGameState(string gameId, string playerId)
        {
            var state = await _sessionService.GetGameState(gameId);
            if (state == null)
            {
                Console.WriteLine($"[GameApi] Game not found: {gameId}");
                return NotFound();
            }

            if (!state.PlayerDecks.ContainsKey(playerId))
            {
                Console.WriteLine($"[GameApi] Player {playerId} not found in game {gameId}");
                return BadRequest($"Player {playerId} is not part of the game.");
            }

            return Ok(state);
        }

        [HttpPost("create")]
        public async Task<ActionResult<string>> CreateGame([FromBody] GameStartRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Player1))
                {
                    Console.WriteLine("[CreateGame] ❌ Player1 is null or empty.");
                    return BadRequest("Player1 is required.");
                }

                Console.WriteLine($"[CreateGame] Creating game for Player1: {request.Player1}");

                // Start with empty decks; deck is uploaded later via /uploadDeck
                var emptyCivic = new List<int>();
                var emptyMilitary = new List<int>();

                var gameId = await _sessionService.CreateGameSession(request.Player1, emptyCivic, emptyMilitary);

                Console.WriteLine($"[CreateGame] ✅ Game created with ID: {gameId}");

                return Ok(gameId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreateGame] ❌ ERROR: {ex.Message}");
                return StatusCode(500, "Game creation failed: " + ex.Message);
            }
        }


        [HttpPost("move")]
        public async Task<IActionResult> SubmitMove([FromBody] GameMove move, [FromQuery] string gameId)
        {
            var result = await _sessionService.ApplyMove(gameId, move);
            return result ? Ok() : BadRequest("Invalid move");
        }

        [HttpPost("join/{gameId}/{playerId}")]
        public async Task<IActionResult> JoinGame(string gameId, string playerId, [FromBody] PlayerDeck playerDeck)
        {
            var gameState = await _sessionService.GetGameState(gameId);

            if (gameState == null)
                return NotFound("Game not found.");

            gameState.PlayerDecks[playerId] = playerDeck;
            await _sessionService.ApplyMove(gameId, new GameMove
            {
                PlayerId = playerId,
                MoveType = "JoinGame"
            });

            return Ok(gameId);
        }

        [HttpPost("uploadDeck/{gameId}")]
        public async Task<IActionResult> UploadDeck(string gameId, [FromForm] IFormFile deckCsv, [FromForm] string playerName)
        {
            try
            {
                if (deckCsv == null || string.IsNullOrWhiteSpace(playerName))
                    return BadRequest("Missing file or playerName.");

                using var reader = new StreamReader(deckCsv.OpenReadStream());
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                // Ensure CsvHelper doesn't try to map to a model automatically
                csv.Context.RegisterClassMap<RawDeckEntryMap>();
                var entries = csv.GetRecords<RawDeckEntry>().ToList();

                var civic = new List<int>();
                var military = new List<int>();

                foreach (var entry in entries)
                {
                    // TEMPORARY logic: assume even Card ID = Civic, odd = Military
                    var targetDeck = entry.CardId % 2 == 0 ? civic : military;

                    for (int i = 0; i < entry.Count; i++)
                    {
                        targetDeck.Add(entry.CardId);
                    }
                }

                var playerDeck = new PlayerDeck(civic, military);

                // TODO: Store playerDeck in your game state using gameId + playerName

                Console.WriteLine($"Uploaded deck for {playerName}: {civic.Count} civic, {military.Count} military");

                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UploadDeck] ERROR: {ex.Message}");
                return StatusCode(500, "Deck upload failed: " + ex.Message);
            }
        }

    }
}
