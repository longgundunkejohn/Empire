using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Empire.Shared.Models;
using Empire.Server.Services;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace Empire.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LobbyController : ControllerBase
    {
        private readonly ILobbyService _lobbyService;
        private readonly ILogger<LobbyController> _logger;

        public LobbyController(ILobbyService lobbyService, ILogger<LobbyController> logger)
        {
            _lobbyService = lobbyService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<LobbyListItem>>> GetActiveLobbies()
        {
            try
            {
                var lobbies = await _lobbyService.GetActiveLobbiesAsync();
                return Ok(lobbies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active lobbies");
                return StatusCode(500, new { message = "Failed to retrieve lobbies" });
            }
        }

        [HttpGet("{lobbyId}")]
        public async Task<ActionResult<GameLobby>> GetLobby(string lobbyId)
        {
            try
            {
                var lobby = await _lobbyService.GetLobbyAsync(lobbyId);
                if (lobby == null)
                {
                    return NotFound(new { message = "Lobby not found" });
                }

                return Ok(lobby);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lobby {LobbyId}", lobbyId);
                return StatusCode(500, new { message = "Failed to retrieve lobby" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<GameLobby>> CreateLobby([FromBody] CreateLobbyRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetCurrentUserId();
                var username = GetCurrentUsername();

                if (userId == 0 || string.IsNullOrEmpty(username))
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                var lobby = await _lobbyService.CreateLobbyAsync(request, userId, username);
                
                _logger.LogInformation("User {UserId} created lobby {LobbyId}", userId, lobby.Id);
                
                return CreatedAtAction(nameof(GetLobby), new { lobbyId = lobby.Id }, lobby);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lobby");
                return StatusCode(500, new { message = "Failed to create lobby" });
            }
        }

        [HttpPost("{lobbyId}/join")]
        public async Task<ActionResult> JoinLobby(string lobbyId, [FromBody] JoinLobbyRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetCurrentUserId();
                var username = GetCurrentUsername();

                if (userId == 0 || string.IsNullOrEmpty(username))
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                var success = await _lobbyService.JoinLobbyAsync(
                    lobbyId, userId, username, request.DeckName, request.PreferredSlot);

                if (!success)
                {
                    return BadRequest(new { message = "Failed to join lobby" });
                }

                var lobby = await _lobbyService.GetLobbyAsync(lobbyId);
                return Ok(new { message = "Successfully joined lobby", lobby });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining lobby {LobbyId}", lobbyId);
                return StatusCode(500, new { message = "Failed to join lobby" });
            }
        }

        [HttpPost("join")]
        public async Task<ActionResult> JoinLobbyBySlot([FromBody] JoinLobbyBySlotRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetCurrentUserId();
                var username = GetCurrentUsername();

                if (userId == 0 || string.IsNullOrEmpty(username))
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                var success = await _lobbyService.JoinLobbyAsync(
                    request.LobbyId, userId, username, null, (PlayerSlot)request.PlayerSlot);

                if (!success)
                {
                    return BadRequest(new { message = "Failed to join lobby" });
                }

                var lobby = await _lobbyService.GetLobbyAsync(request.LobbyId);
                return Ok(new { message = "Successfully joined lobby", lobby });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining lobby {LobbyId}", request.LobbyId);
                return StatusCode(500, new { message = "Failed to join lobby" });
            }
        }

        [HttpPost("spectate")]
        public async Task<ActionResult> JoinAsSpectator([FromBody] JoinSpectatorRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetCurrentUserId();
                var username = GetCurrentUsername();

                if (userId == 0 || string.IsNullOrEmpty(username))
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                var success = await _lobbyService.JoinAsSpectatorAsync(request.LobbyId, userId, username);

                if (!success)
                {
                    return BadRequest(new { message = "Failed to join as spectator" });
                }

                var lobby = await _lobbyService.GetLobbyAsync(request.LobbyId);
                return Ok(new { message = "Successfully joined as spectator", lobby });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error spectating lobby {LobbyId}", request.LobbyId);
                return StatusCode(500, new { message = "Failed to join as spectator" });
            }
        }

        [HttpPost("ready")]
        public async Task<ActionResult> SetPlayerReady([FromBody] SetPlayerReadyRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetCurrentUserId();

                if (userId == 0)
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                var success = await _lobbyService.SetPlayerReadyAsync(request.LobbyId, userId, request.Ready);

                if (!success)
                {
                    return BadRequest(new { message = "Failed to update ready status" });
                }

                var lobby = await _lobbyService.GetLobbyAsync(request.LobbyId);
                return Ok(new { message = "Ready status updated successfully", lobby });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ready status in lobby {LobbyId}", request.LobbyId);
                return StatusCode(500, new { message = "Failed to update ready status" });
            }
        }

        [HttpPost("{lobbyId}/spectate")]
        public async Task<ActionResult> SpectateGame(string lobbyId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var username = GetCurrentUsername();

                if (userId == 0 || string.IsNullOrEmpty(username))
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                var success = await _lobbyService.JoinAsSpectatorAsync(lobbyId, userId, username);

                if (!success)
                {
                    return BadRequest(new { message = "Failed to join as spectator" });
                }

                var lobby = await _lobbyService.GetLobbyAsync(lobbyId);
                return Ok(new { message = "Successfully joined as spectator", lobby });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error spectating lobby {LobbyId}", lobbyId);
                return StatusCode(500, new { message = "Failed to join as spectator" });
            }
        }

        [HttpPost("{lobbyId}/leave")]
        public async Task<ActionResult> LeaveLobby(string lobbyId)
        {
            try
            {
                var userId = GetCurrentUserId();

                if (userId == 0)
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                var success = await _lobbyService.LeaveLobbyAsync(lobbyId, userId);

                if (!success)
                {
                    return BadRequest(new { message = "Failed to leave lobby" });
                }

                return Ok(new { message = "Successfully left lobby" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving lobby {LobbyId}", lobbyId);
                return StatusCode(500, new { message = "Failed to leave lobby" });
            }
        }

        [HttpPost("{lobbyId}/start")]
        public async Task<ActionResult> StartGame(string lobbyId)
        {
            try
            {
                var userId = GetCurrentUserId();

                if (userId == 0)
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                var success = await _lobbyService.StartGameAsync(lobbyId, userId);

                if (!success)
                {
                    return BadRequest(new { message = "Failed to start game" });
                }

                var lobby = await _lobbyService.GetLobbyAsync(lobbyId);
                
                // Notify all clients in the lobby that the game has started
                // This will be handled by SignalR injection in the future
                // For now, the client will handle the transition
                
                return Ok(new { 
                    message = "Game started successfully", 
                    lobby,
                    gameId = lobbyId // The lobby ID becomes the game ID
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting game in lobby {LobbyId}", lobbyId);
                return StatusCode(500, new { message = "Failed to start game" });
            }
        }

        [HttpPost("{lobbyId}/cancel")]
        public async Task<ActionResult> CancelLobby(string lobbyId)
        {
            try
            {
                var userId = GetCurrentUserId();

                if (userId == 0)
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                var success = await _lobbyService.CancelLobbyAsync(lobbyId, userId);

                if (!success)
                {
                    return BadRequest(new { message = "Failed to cancel lobby" });
                }

                return Ok(new { message = "Lobby cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling lobby {LobbyId}", lobbyId);
                return StatusCode(500, new { message = "Failed to cancel lobby" });
            }
        }

        [HttpPut("{lobbyId}/deck")]
        public async Task<ActionResult> UpdateDeck(string lobbyId, [FromBody] UpdateDeckRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetCurrentUserId();

                if (userId == 0)
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                // Validate deck
                var validationErrors = await _lobbyService.ValidateDeckAsync(request.DeckName, userId);
                if (validationErrors.Any())
                {
                    return BadRequest(new { message = "Deck validation failed", errors = validationErrors });
                }

                var success = await _lobbyService.UpdatePlayerDeckAsync(lobbyId, userId, request.DeckName);

                if (!success)
                {
                    return BadRequest(new { message = "Failed to update deck" });
                }

                var lobby = await _lobbyService.GetLobbyAsync(lobbyId);
                return Ok(new { message = "Deck updated successfully", lobby });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating deck in lobby {LobbyId}", lobbyId);
                return StatusCode(500, new { message = "Failed to update deck" });
            }
        }

        [HttpPost("validate-deck")]
        public async Task<ActionResult> ValidateDeck([FromBody] ValidateDeckRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetCurrentUserId();

                if (userId == 0)
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                var errors = await _lobbyService.ValidateDeckAsync(request.DeckName, userId);
                var isValid = !errors.Any();

                return Ok(new { isValid, errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating deck");
                return StatusCode(500, new { message = "Failed to validate deck" });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetCurrentUsername()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        }
    }

    // Additional DTOs for API requests
    public class UpdateDeckRequest
    {
        [Required]
        public string DeckName { get; set; } = string.Empty;
    }

    public class ValidateDeckRequest
    {
        [Required]
        public string DeckName { get; set; } = string.Empty;
    }

    public class JoinLobbyBySlotRequest
    {
        [Required]
        public string LobbyId { get; set; } = string.Empty;
        
        [Required]
        public int PlayerSlot { get; set; }
    }

    public class JoinSpectatorRequest
    {
        [Required]
        public string LobbyId { get; set; } = string.Empty;
    }

    public class SetPlayerReadyRequest
    {
        [Required]
        public string LobbyId { get; set; } = string.Empty;
        
        [Required]
        public bool Ready { get; set; }
    }
}
