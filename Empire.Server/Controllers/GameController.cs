// 🔧 FILE: GameController.cs (Empire.Server/Controllers)
using Empire.Shared.Models;
using Empire.Shared.Models.DTOs;
using Empire.Shared.Models.Enums;
using Empire.Server.Services;
using Empire.Server.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Empire.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly DeckLoaderService _deckLoader;
        private readonly ICardDatabaseService _cardService;
        private readonly GameStateService _gameStateService;
        private readonly IHubContext<GameHub> _hubContext;
        private readonly ILogger<GameController> _logger;
        private static readonly ConcurrentDictionary<string, GameState> _gameStates = new();

        public GameController(
            DeckLoaderService deckLoader, 
            ICardDatabaseService cardService, 
            GameStateService gameStateService,
            IHubContext<GameHub> hubContext,
            ILogger<GameController> logger)
        {
            _deckLoader = deckLoader;
            _cardService = cardService;
            _gameStateService = gameStateService;
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateGame([FromBody] GameStartRequest request)
        {
            try
            {
                var playerDeck = _deckLoader.LoadDeck(request.Player1);
                if (playerDeck.CivicDeck.Count == 0 && playerDeck.MilitaryDeck.Count == 0)
                    return NotFound("No deck found for this player.");

                var civicCards = await _cardService.GetDeckCards(playerDeck.CivicDeck);
                var militaryCards = await _cardService.GetDeckCards(playerDeck.MilitaryDeck);
                var allCards = civicCards.Concat(militaryCards).ToList();

                var hand = allCards.Take(5).Select(c => c.CardId).ToList();

                var gameId = Guid.NewGuid().ToString();
                var game = new GameState
                {
                    GameId = gameId,
                    Player1 = request.Player1,
                    CurrentPhase = GamePhase.Strategy,
                    PlayerDecks = new Dictionary<string, List<Card>> { [request.Player1] = allCards },
                    PlayerHands = new Dictionary<string, List<int>> { [request.Player1] = hand },
                    PlayerBoard = new Dictionary<string, List<BoardCard>> { [request.Player1] = new() },
                    PlayerGraveyards = new Dictionary<string, List<int>> { [request.Player1] = new() },
                    PlayerLifeTotals = new Dictionary<string, int> { [request.Player1] = 20 },
                    PlayerSealedZones = new Dictionary<string, List<int>> { [request.Player1] = new() }
                };

                _gameStates.TryAdd(gameId, game);
                _logger.LogInformation("✅ Created game {GameId} for player {Player}", gameId, request.Player1);

                return Ok(gameId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating game for player {Player}", request.Player1);
                return StatusCode(500, "Error creating game");
            }
        }

        [HttpGet("open")]
        public async Task<ActionResult<List<GamePreview>>> GetOpenGames()
        {
            try
            {
                var openGames = _gameStates.Values
                    .Where(g => !string.IsNullOrEmpty(g.Player1) && string.IsNullOrEmpty(g.Player2))
                    .Select(g => new GamePreview
                    {
                        GameId = g.GameId,
                        HostPlayer = g.Player1,
                        IsJoinable = true
                    }).ToList();

                return Ok(openGames);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting open games");
                return StatusCode(500, "Error retrieving open games");
            }
        }

        [HttpPost("{gameId}/join/{playerId}")]
        public async Task<IActionResult> JoinGame(string gameId, string playerId, [FromBody] JoinGameRequest deck)
        {
            try
            {
                if (!_gameStates.TryGetValue(gameId, out var game))
                    return NotFound("Game not found");

                if (!string.IsNullOrEmpty(game.Player2))
                    return BadRequest("Game already full");

                var civicCards = await _cardService.GetDeckCards(deck.CivicDeck);
                var militaryCards = await _cardService.GetDeckCards(deck.MilitaryDeck);
                var allCards = civicCards.Concat(militaryCards).ToList();

                var hand = allCards.Take(5).Select(c => c.CardId).ToList();

                game.Player2 = playerId;
                game.PlayerDecks[playerId] = allCards;
                game.PlayerHands[playerId] = hand;
                game.PlayerBoard[playerId] = new();
                game.PlayerGraveyards[playerId] = new();
                game.PlayerLifeTotals[playerId] = 20;
                game.PlayerSealedZones[playerId] = new();

                _logger.LogInformation("✅ Player {Player} joined game {GameId}", playerId, gameId);
                return Ok(new { message = $"✅ {playerId} joined game {gameId}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error joining game {GameId} for player {Player}", gameId, playerId);
                return StatusCode(500, "Error joining game");
            }
        }

        [HttpPost("{gameId}/draw/{playerId}/{type}")]
        public async Task<ActionResult<int>> DrawCard(string gameId, string playerId, string type)
        {
            try
            {
                if (!_gameStates.TryGetValue(gameId, out var game))
                    return NotFound("Game not found");

                if (!game.PlayerDecks.TryGetValue(playerId, out var deck))
                    return BadRequest("Deck not found for player");

                var drawPool = deck.Where(c =>
                    type.ToLower() == "civic" ? DeckUtils.IsCivicCard(c.CardId) : !DeckUtils.IsCivicCard(c.CardId)).ToList();

                if (!drawPool.Any()) return BadRequest("No cards left of type");

                var card = drawPool.First();
                deck.Remove(card);

                if (!game.PlayerHands.ContainsKey(playerId))
                    game.PlayerHands[playerId] = new();

                game.PlayerHands[playerId].Add(card.CardId);

                return Ok(card.CardId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error drawing card for player {Player} in game {GameId}", playerId, gameId);
                return StatusCode(500, "Error drawing card");
            }
        }

        [HttpGet("{gameId}/state")]
        public async Task<ActionResult<GameState>> GetGameState(string gameId)
        {
            try
            {
                if (!_gameStates.TryGetValue(gameId, out var state))
                    return NotFound("Game not found");

                return Ok(state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting game state for {GameId}", gameId);
                return StatusCode(500, "Error retrieving game state");
            }
        }

        [HttpPost("{gameId}/move")]
        public async Task<IActionResult> SubmitMove(string gameId, [FromBody] GameMove move)
        {
            try
            {
                if (!_gameStates.TryGetValue(gameId, out var game))
                    return NotFound("Game not found");

                // Add move to history
                game.MoveHistory.Add(move);

                // Process the move based on type
                switch (move.MoveType?.ToLowerInvariant())
                {
                    case "shuffledeck":
                        await ProcessShuffleDeck(game, move.PlayerId);
                        break;
                    case "playcard":
                        if (move.CardId.HasValue)
                            await ProcessPlayCard(game, move.PlayerId, move.CardId.Value);
                        break;
                    case "exertcard":
                        if (move.CardId.HasValue)
                            await ProcessExertCard(game, move.PlayerId, move.CardId.Value);
                        break;
                    case "endturn":
                        await ProcessEndTurn(game, move.PlayerId);
                        break;
                    default:
                        return BadRequest($"Unknown move type: {move.MoveType}");
                }

                // Notify clients about the move and game state update
                await _hubContext.Clients.Group(gameId).SendAsync("MoveSubmitted", move);
                await _hubContext.Clients.Group(gameId).SendAsync("GameStateUpdated", gameId);

                return Ok(new { message = $"Move {move.MoveType} processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error submitting move for game {GameId}", gameId);
                return StatusCode(500, "Error processing move");
            }
        }

        private async Task ProcessShuffleDeck(GameState game, string playerId)
        {
            if (game.PlayerDecks.TryGetValue(playerId, out var deck))
            {
                // Simple shuffle using Random
                var random = new Random();
                for (int i = deck.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    (deck[i], deck[j]) = (deck[j], deck[i]);
                }
            }
            await Task.CompletedTask;
        }

        private async Task ProcessPlayCard(GameState game, string playerId, int cardId)
        {
            // Move card from hand to board
            if (game.PlayerHands.TryGetValue(playerId, out var hand) && hand.Contains(cardId))
            {
                hand.Remove(cardId);
                
                if (!game.PlayerBoard.ContainsKey(playerId))
                    game.PlayerBoard[playerId] = new List<BoardCard>();
                
                game.PlayerBoard[playerId].Add(new BoardCard(cardId));
            }
            await Task.CompletedTask;
        }

        private async Task ProcessExertCard(GameState game, string playerId, int cardId)
        {
            // Find card on board and mark as exerted
            if (game.PlayerBoard.TryGetValue(playerId, out var board))
            {
                var boardCard = board.FirstOrDefault(bc => bc.CardId == cardId);
                if (boardCard != null)
                {
                    boardCard.IsExerted = true;
                }
            }
            await Task.CompletedTask;
        }

        private async Task ProcessEndTurn(GameState game, string playerId)
        {
            // Switch active player and advance phase
            if (game.Player1 == playerId)
            {
                game.PriorityPlayer = game.Player2;
            }
            else if (game.Player2 == playerId)
            {
                game.PriorityPlayer = game.Player1;
            }

            // Advance game phase
            game.CurrentPhase = game.CurrentPhase switch
            {
                GamePhase.Strategy => GamePhase.Battle,
                GamePhase.Battle => GamePhase.Replenishment,
                GamePhase.Replenishment => GamePhase.Strategy,
                _ => GamePhase.Strategy
            };

            // Unexert all cards for the player ending their turn
            if (game.PlayerBoard.TryGetValue(playerId, out var board))
            {
                foreach (var card in board)
                {
                    card.IsExerted = false;
                }
            }

            await Task.CompletedTask;
        }

        // Empire-specific action endpoints
        
        [HttpPost("{gameId}/empire/create")]
        public async Task<IActionResult> CreateEmpireGame(string gameId, [FromBody] EmpireGameStartRequest request)
        {
            try
            {
                await _gameStateService.LoadGameState(gameId);
                await _gameStateService.InitializeEmpireGame(gameId, request.Player1Id, request.Player2Id);
                
                // Notify clients that the game has started
                await _hubContext.Clients.Group(gameId).SendAsync("GameStarted", gameId);
                await _hubContext.Clients.Group(gameId).SendAsync("GameStateUpdated", gameId);
                
                return Ok(new { message = "Empire game initialized successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating Empire game {GameId}", gameId);
                return BadRequest($"Failed to initialize Empire game: {ex.Message}");
            }
        }

        [HttpPost("{gameId}/empire/setup-deck/{playerId}")]
        public async Task<IActionResult> SetupPlayerDeck(string gameId, string playerId, [FromBody] EmpireDeckSetupRequest request)
        {
            try
            {
                await _gameStateService.LoadGameState(gameId);
                
                // Get cards from database
                var armyCards = await _cardService.GetDeckCards(request.ArmyDeckIds);
                var civicCards = await _cardService.GetDeckCards(request.CivicDeckIds);
                
                await _gameStateService.SetupPlayerDecks(playerId, armyCards.ToList(), civicCards.ToList());
                
                await _hubContext.Clients.Group(gameId).SendAsync("GameStateUpdated", gameId);
                
                return Ok(new { message = "Player deck setup successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error setting up deck for player {Player} in game {GameId}", playerId, gameId);
                return BadRequest($"Failed to setup player deck: {ex.Message}");
            }
        }

        [HttpPost("{gameId}/empire/deploy-army")]
        public async Task<IActionResult> DeployArmyCard(string gameId, [FromBody] DeployArmyCardRequest request)
        {
            try
            {
                await _gameStateService.LoadGameState(gameId);
                
                bool success = await _gameStateService.DeployArmyCard(request.PlayerId, request.CardId, request.ManaCost);
                if (!success)
                {
                    return BadRequest("Cannot deploy army card - invalid action or insufficient resources");
                }
                
                // Notify clients
                await _hubContext.Clients.Group(gameId).SendAsync("ActionTaken", request.PlayerId, "DeployArmyCard", new { request.CardId, request.ManaCost });
                await _hubContext.Clients.Group(gameId).SendAsync("InitiativePassed", request.PlayerId);
                await _hubContext.Clients.Group(gameId).SendAsync("GameStateUpdated", gameId);
                
                return Ok(new { message = "Army card deployed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deploying army card for player {Player} in game {GameId}", request.PlayerId, gameId);
                return BadRequest($"Failed to deploy army card: {ex.Message}");
            }
        }

        [HttpPost("{gameId}/empire/play-villager")]
        public async Task<IActionResult> PlayVillager(string gameId, [FromBody] PlayVillagerRequest request)
        {
            try
            {
                await _gameStateService.LoadGameState(gameId);
                
                bool success = await _gameStateService.PlayVillager(request.PlayerId, request.CardId);
                if (!success)
                {
                    return BadRequest("Cannot play villager - invalid action or already played this round");
                }
                
                // Notify clients
                await _hubContext.Clients.Group(gameId).SendAsync("ActionTaken", request.PlayerId, "PlayVillager", new { request.CardId });
                await _hubContext.Clients.Group(gameId).SendAsync("InitiativePassed", request.PlayerId);
                await _hubContext.Clients.Group(gameId).SendAsync("GameStateUpdated", gameId);
                
                return Ok(new { message = "Villager played successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error playing villager for player {Player} in game {GameId}", request.PlayerId, gameId);
                return BadRequest($"Failed to play villager: {ex.Message}");
            }
        }

        [HttpPost("{gameId}/empire/settle-territory")]
        public async Task<IActionResult> SettleTerritory(string gameId, [FromBody] SettleTerritoryRequest request)
        {
            try
            {
                await _gameStateService.LoadGameState(gameId);
                
                bool success = await _gameStateService.SettleTerritory(request.PlayerId, request.CardId, request.TerritoryId);
                if (!success)
                {
                    return BadRequest("Cannot settle territory - invalid action or not occupying territory");
                }
                
                // Notify clients
                await _hubContext.Clients.Group(gameId).SendAsync("ActionTaken", request.PlayerId, "SettleTerritory", new { request.CardId, request.TerritoryId });
                await _hubContext.Clients.Group(gameId).SendAsync("InitiativePassed", request.PlayerId);
                await _hubContext.Clients.Group(gameId).SendAsync("GameStateUpdated", gameId);
                
                return Ok(new { message = "Territory settled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error settling territory for player {Player} in game {GameId}", request.PlayerId, gameId);
                return BadRequest($"Failed to settle territory: {ex.Message}");
            }
        }

        [HttpPost("{gameId}/empire/commit-units")]
        public async Task<IActionResult> CommitUnits(string gameId, [FromBody] CommitUnitsRequest request)
        {
            try
            {
                await _gameStateService.LoadGameState(gameId);
                
                bool success = await _gameStateService.CommitUnits(request.PlayerId, request.UnitCommitments);
                if (!success)
                {
                    return BadRequest("Cannot commit units - invalid action or already committed this round");
                }
                
                // Notify clients
                await _hubContext.Clients.Group(gameId).SendAsync("ActionTaken", request.PlayerId, "CommitUnits", request.UnitCommitments);
                await _hubContext.Clients.Group(gameId).SendAsync("InitiativePassed", request.PlayerId);
                await _hubContext.Clients.Group(gameId).SendAsync("GameStateUpdated", gameId);
                
                return Ok(new { message = "Units committed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error committing units for player {Player} in game {GameId}", request.PlayerId, gameId);
                return BadRequest($"Failed to commit units: {ex.Message}");
            }
        }

        [HttpPost("{gameId}/empire/pass-initiative")]
        public async Task<IActionResult> PassInitiative(string gameId, [FromBody] PassInitiativeRequest request)
        {
            try
            {
                await _gameStateService.LoadGameState(gameId);
                
                bool success = await _gameStateService.PassInitiative(request.PlayerId);
                if (!success)
                {
                    return BadRequest("Cannot pass initiative - not your turn");
                }
                
                // Check if phase should advance
                var gameState = _gameStateService.GameState;
                if (gameState.LastPlayerToPass != null)
                {
                    // Both players passed, phase will advance
                    await _hubContext.Clients.Group(gameId).SendAsync("PhaseTransition", gameState.CurrentPhase.ToString(), gameState.InitiativeHolder);
                }
                else
                {
                    // Just pass initiative
                    await _hubContext.Clients.Group(gameId).SendAsync("PlayerPassed", request.PlayerId);
                    await _hubContext.Clients.Group(gameId).SendAsync("InitiativePassed", request.PlayerId);
                }
                
                await _hubContext.Clients.Group(gameId).SendAsync("GameStateUpdated", gameId);
                
                return Ok(new { message = "Initiative passed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error passing initiative for player {Player} in game {GameId}", request.PlayerId, gameId);
                return BadRequest($"Failed to pass initiative: {ex.Message}");
            }
        }

        [HttpPost("{gameId}/empire/draw-cards")]
        public async Task<IActionResult> DrawCards(string gameId, [FromBody] DrawCardsRequest request)
        {
            try
            {
                await _gameStateService.LoadGameState(gameId);
                
                if (request.DrawArmy)
                {
                    await _gameStateService.DrawArmyCard(request.PlayerId);
                }
                else
                {
                    await _gameStateService.DrawCivicCards(request.PlayerId, 2);
                }
                
                await _hubContext.Clients.Group(gameId).SendAsync("GameStateUpdated", gameId);
                
                return Ok(new { message = "Cards drawn successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error drawing cards for player {Player} in game {GameId}", request.PlayerId, gameId);
                return BadRequest($"Failed to draw cards: {ex.Message}");
            }
        }

        [HttpPost("{gameId}/empire/update-morale")]
        public async Task<IActionResult> UpdateMorale(string gameId, [FromBody] UpdateMoraleRequest request)
        {
            try
            {
                await _gameStateService.LoadGameState(gameId);
                
                await _gameStateService.UpdateMorale(request.PlayerId, request.Damage);
                
                var newMorale = _gameStateService.GameState.PlayerMorale[request.PlayerId];
                
                // Notify clients
                await _hubContext.Clients.Group(gameId).SendAsync("MoraleUpdated", request.PlayerId, newMorale, request.Damage);
                await _hubContext.Clients.Group(gameId).SendAsync("GameStateUpdated", gameId);
                
                // Check for game over
                if (_gameStateService.IsGameOver())
                {
                    var winner = _gameStateService.GetWinner();
                    await _hubContext.Clients.Group(gameId).SendAsync("GameEnded", winner);
                }
                
                return Ok(new { message = "Morale updated successfully", newMorale });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating morale for player {Player} in game {GameId}", request.PlayerId, gameId);
                return BadRequest($"Failed to update morale: {ex.Message}");
            }
        }
    }

    // Empire-specific request DTOs
    public class EmpireGameStartRequest
    {
        public string Player1Id { get; set; } = string.Empty;
        public string Player2Id { get; set; } = string.Empty;
    }

    public class EmpireDeckSetupRequest
    {
        public List<int> ArmyDeckIds { get; set; } = new();
        public List<int> CivicDeckIds { get; set; } = new();
    }

    public class DeployArmyCardRequest
    {
        public string PlayerId { get; set; } = string.Empty;
        public int CardId { get; set; }
        public int ManaCost { get; set; }
    }

    public class PlayVillagerRequest
    {
        public string PlayerId { get; set; } = string.Empty;
        public int CardId { get; set; }
    }

    public class SettleTerritoryRequest
    {
        public string PlayerId { get; set; } = string.Empty;
        public int CardId { get; set; }
        public string TerritoryId { get; set; } = string.Empty;
    }

    public class CommitUnitsRequest
    {
        public string PlayerId { get; set; } = string.Empty;
        public Dictionary<int, string> UnitCommitments { get; set; } = new();
    }

    public class PassInitiativeRequest
    {
        public string PlayerId { get; set; } = string.Empty;
    }

    public class DrawCardsRequest
    {
        public string PlayerId { get; set; } = string.Empty;
        public bool DrawArmy { get; set; } = true;
    }

    public class UpdateMoraleRequest
    {
        public string PlayerId { get; set; } = string.Empty;
        public int Damage { get; set; }
    }
}
