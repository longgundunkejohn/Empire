using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Empire.Server.Services;
using Empire.Shared.Models;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace Empire.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LobbyController : ControllerBase
    {
        private readonly LobbyService _lobbyService;
        private readonly ILogger<LobbyController> _logger;

        public LobbyController(LobbyService lobbyService, ILogger<LobbyController> logger)
        {
            _lobbyService = lobbyService;
            _logger = logger;
        }

        [HttpGet("list")]
        [AllowAnonymous]
        public async Task<ActionResult> GetLobbyList()
        {
            try
            {
                // For now, return mock data until LobbyService is fully implemented
                var mockLobbies = new List<LobbyListItem>
                {
                    new LobbyListItem
                    {
                        Id = "lobby-1",
                        Name = "Epic Empire Battle",
                        HostUsername = "Player1",
                        PlayerCount = 1,
                        SpectatorCount = 0,
                        Status = LobbyStatus.WaitingForPlayers,
                        AllowSpectators = true,
                        CreatedDate = DateTime.UtcNow.AddMinutes(-5),
                        IsFull = false,
                        CanJoin = true
                    },
                    new LobbyListItem
                    {
                        Id = "lobby-2",
                        Name = "Competitive Match",
                        HostUsername = "ProPlayer",
                        PlayerCount = 2,
                        SpectatorCount = 1,
                        Status = LobbyStatus.InProgress,
                        AllowSpectators = true,
                        CreatedDate = DateTime.UtcNow.AddMinutes(-15),
                        IsFull = true,
                        CanJoin = false
                    }
                };
                
                return Ok(mockLobbies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lobby list");
                return StatusCode(500, new { message = "Failed to get lobby list" });
            }
        }

        [HttpPost("create")]
        public async Task<ActionResult> CreateLobby([FromBody] CreateLobbyRequest request)
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

                // For now, create a mock lobby until LobbyService is fully implemented
                var lobby = new GameLobby
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = request.Name,
                    HostUserId = userId,
                    HostUsername = username,
                    Status = LobbyStatus.WaitingForPlayers,
                    AllowSpectators = request.AllowSpectators,
                    MaxSpectators = request.MaxSpectators,
                    RequireDeckValidation = request.RequireDeckValidation,
                    TimeLimit = request.TimeLimit,
                    CreatedDate = DateTime.UtcNow,
                    Spectators = new List<SpectatorInfo>()
                };

                _logger.LogInformation("User {Username} created lobby {LobbyId}", username, lobby.Id);
                return Ok(lobby);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lobby");
                return StatusCode(500, new { message = "Failed to create lobby" });
            }
        }

        [HttpPost("join")]
        public async Task<ActionResult> JoinLobby([FromBody] JoinLobbyRequest request)
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

                // For now, return success until LobbyService is fully implemented
                _logger.LogInformation("User {Username} joined lobby {LobbyId}", username, request.LobbyId);
                return Ok(new { message = "Joined lobby successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining lobby {LobbyId}", request.LobbyId);
                return StatusCode(500, new { message = "Failed to join lobby" });
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

                _logger.LogInformation("User {UserId} left lobby {LobbyId}", userId, lobbyId);
                return Ok(new { message = "Left lobby successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving lobby {LobbyId}", lobbyId);
                return StatusCode(500, new { message = "Failed to leave lobby" });
            }
        }

        [HttpGet("{lobbyId}")]
        public async Task<ActionResult> GetLobby(string lobbyId)
        {
            try
            {
                // For now, return mock data until LobbyService is fully implemented
                var mockLobby = new GameLobby
                {
                    Id = lobbyId,
                    Name = "Test Game Room",
                    HostUserId = 1,
                    HostUsername = "TestHost",
                    Status = LobbyStatus.WaitingForPlayers,
                    AllowSpectators = true,
                    MaxSpectators = 10,
                    RequireDeckValidation = true,
                    TimeLimit = 30,
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    Spectators = new List<SpectatorInfo>()
                };

                return Ok(mockLobby);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lobby {LobbyId}", lobbyId);
                return StatusCode(500, new { message = "Failed to get lobby" });
            }
        }

        [HttpPost("set-ready")]
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

                _logger.LogInformation("User {UserId} set ready status to {Ready} in lobby {LobbyId}", 
                    userId, request.Ready, request.LobbyId);
                return Ok(new { message = "Ready status updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting ready status in lobby {LobbyId}", request.LobbyId);
                return StatusCode(500, new { message = "Failed to update ready status" });
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

                _logger.LogInformation("User {UserId} started game in lobby {LobbyId}", userId, lobbyId);
                return Ok(new { message = "Game started successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting game in lobby {LobbyId}", lobbyId);
                return StatusCode(500, new { message = "Failed to start game" });
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

    // DTOs for API requests - moved here to avoid duplication
    public class CreateLobbyRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;
        
        public bool AllowSpectators { get; set; } = true;
        public int MaxSpectators { get; set; } = 10;
        public bool RequireDeckValidation { get; set; } = true;
        public int TimeLimit { get; set; } = 30;
    }

    public class JoinLobbyRequest
    {
        [Required]
        public string LobbyId { get; set; } = string.Empty;
        
        public string? DeckName { get; set; }
        public PlayerSlot? PreferredSlot { get; set; }
    }

    public class UpdateDeckRequest
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
