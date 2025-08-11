using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Empire.Client.Services
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string username, string password);
        Task<AuthResult> RegisterAsync(string username, string password, string confirmPassword);
        Task<bool> IsAuthenticatedAsync();
        Task<User?> GetCurrentUserAsync();
        Task LogoutAsync();
        Task<string?> GetTokenAsync();
    }

    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<AuthService> _logger;
        private User? _currentUser;
        private string? _token;

        public AuthService(HttpClient httpClient, IJSRuntime jsRuntime, ILogger<AuthService> logger)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        public async Task<AuthResult> LoginAsync(string username, string password)
        {
            try
            {
                var request = new { Username = username, Password = password };
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (loginResponse?.Token != null && loginResponse.User != null)
                    {
                        _token = loginResponse.Token;
                        _currentUser = loginResponse.User;

                        // Store token in localStorage
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "empire_token", _token);
                        
                        // Set authorization header for future requests
                        _httpClient.DefaultRequestHeaders.Authorization = 
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

                        _logger.LogInformation("User {Username} logged in successfully", username);
                        return AuthResult.CreateSuccess("Login successful");
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return AuthResult.CreateFailure(errorResponse?.Message ?? "Login failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return AuthResult.CreateFailure("Login failed due to network error");
            }
        }

        public async Task<AuthResult> RegisterAsync(string username, string password, string confirmPassword)
        {
            try
            {
                var request = new { Username = username, Password = password, ConfirmPassword = confirmPassword };
                var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var registerResponse = JsonSerializer.Deserialize<LoginResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (registerResponse?.Token != null && registerResponse.User != null)
                    {
                        _token = registerResponse.Token;
                        _currentUser = registerResponse.User;

                        // Store token in localStorage
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "empire_token", _token);
                        
                        // Set authorization header for future requests
                        _httpClient.DefaultRequestHeaders.Authorization = 
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

                        _logger.LogInformation("User {Username} registered successfully", username);
                return AuthResult.CreateSuccess("Registration successful");
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return AuthResult.CreateFailure(errorResponse?.Message ?? "Registration failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return AuthResult.CreateFailure("Registration failed due to network error");
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            if (_token != null && _currentUser != null)
                return true;

            // Try to get token from localStorage
            try
            {
                var storedToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "empire_token");
                if (!string.IsNullOrEmpty(storedToken))
                {
                    // Validate token with server
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", storedToken);

                    var response = await _httpClient.PostAsync("api/auth/validate-token", null);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var validationResponse = JsonSerializer.Deserialize<TokenValidationResponse>(content, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (validationResponse?.Valid == true && validationResponse.User != null)
                        {
                            _token = storedToken;
                            _currentUser = validationResponse.User;
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating stored token");
            }

            return false;
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            if (_currentUser != null)
                return _currentUser;

            if (await IsAuthenticatedAsync())
                return _currentUser;

            return null;
        }

        public async Task LogoutAsync()
        {
            _token = null;
            _currentUser = null;
            
            // Remove token from localStorage
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "empire_token");
            
            // Remove authorization header
            _httpClient.DefaultRequestHeaders.Authorization = null;

            _logger.LogInformation("User logged out");
        }

        public async Task<string?> GetTokenAsync()
        {
            if (_token != null)
                return _token;

            if (await IsAuthenticatedAsync())
                return _token;

            return null;
        }
    }

    // Response models
    public class LoginResponse
    {
        public string Message { get; set; } = string.Empty;
        public User? User { get; set; }
        public string? Token { get; set; }
    }

    public class TokenValidationResponse
    {
        public bool Valid { get; set; }
        public User? User { get; set; }
    }

    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime LastLoginDate { get; set; }
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public static AuthResult CreateSuccess(string message)
        {
            return new AuthResult { Success = true, Message = message };
        }

        public static AuthResult CreateFailure(string message)
        {
            return new AuthResult { Success = false, Message = message };
        }
    }
}
