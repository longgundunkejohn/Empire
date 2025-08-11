using Microsoft.AspNetCore.Mvc;
using Empire.Server.Services;
using System.ComponentModel.DataAnnotations;

namespace Empire.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthenticationService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RegisterAsync(request.Username, request.Password);

                if (!result.Success)
                {
                    return BadRequest(new { message = result.ErrorMessage });
                }

                return Ok(new
                {
                    message = "Registration successful",
                    user = new
                    {
                        id = result.User!.Id,
                        username = result.User.Username,
                        createdDate = result.User.CreatedDate
                    },
                    token = result.Token
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.LoginAsync(request.Username, request.Password);

                if (!result.Success)
                {
                    return Unauthorized(new { message = result.ErrorMessage });
                }

                return Ok(new
                {
                    message = "Login successful",
                    user = new
                    {
                        id = result.User!.Id,
                        username = result.User.Username,
                        lastLoginDate = result.User.LastLoginDate
                    },
                    token = result.Token
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("validate-token")]
        public IActionResult ValidateToken()
        {
            // If the request reaches here, the JWT middleware has already validated the token
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            return Ok(new
            {
                valid = true,
                user = new
                {
                    id = int.Parse(userId),
                    username = username
                }
            });
        }
    }

    public class RegisterRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
