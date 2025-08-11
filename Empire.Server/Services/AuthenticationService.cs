using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Empire.Server.Models;
using Empire.Server.Data;
using Empire.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Empire.Server.Services
{
    public interface IAuthenticationService
    {
        Task<AuthResult> RegisterAsync(string username, string password);
        Task<AuthResult> LoginAsync(string username, string password);
        string GenerateJwtToken(User user);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly EmpireDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(EmpireDbContext context, IConfiguration configuration, ILogger<AuthenticationService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResult> RegisterAsync(string username, string password)
        {
            try
            {
                // Check if username already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

                if (existingUser != null)
                {
                return AuthResult.CreateFailure("Username already exists");
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
                {
                    return AuthResult.CreateFailure("Username must be at least 3 characters long");
                }

                if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                {
                    return AuthResult.CreateFailure("Password must be at least 6 characters long");
                }

                // Create new user
                var user = new User
                {
                    Username = username.Trim(),
                    PasswordHash = HashPassword(password),
                    CreatedDate = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {Username} registered successfully", username);

                var token = GenerateJwtToken(user);
                return AuthResult.CreateSuccess(user, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user {Username}", username);
                return AuthResult.CreateFailure("Registration failed");
            }
        }

        public async Task<AuthResult> LoginAsync(string username, string password)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

                if (user == null)
                {
                    return AuthResult.CreateFailure("Invalid username or password");
                }

                if (!VerifyPassword(password, user.PasswordHash))
                {
                    return AuthResult.CreateFailure("Invalid username or password");
                }

                // Update last login date
                user.LastLoginDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {Username} logged in successfully", username);

                var token = GenerateJwtToken(user);
                return AuthResult.CreateSuccess(user, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging in user {Username}", username);
                return AuthResult.CreateFailure("Login failed");
            }
        }

        public string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
            var issuer = jwtSettings["Issuer"] ?? "EmpireTCG";
            var audience = jwtSettings["Audience"] ?? "EmpireTCGUsers";
            var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "1440"); // 24 hours default

            var key = Encoding.ASCII.GetBytes(secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim("username", user.Username)
                }),
                Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string HashPassword(string password)
        {
            // Using BCrypt for password hashing
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        public bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }
    }

}
