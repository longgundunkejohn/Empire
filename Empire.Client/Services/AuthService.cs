using System.Net.Http.Json;
using System.Text.Json;
using System.Security.Claims;

namespace Empire.Client.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private string? _currentUsername;
        private int _currentUserId;
        private bool _isAuthenticated;

        public AuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<AuthResult> LoginAsync(string username, string password)
        {
            try
            {
                var loginRequest = new { Username = username, Password = password };
                var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AuthResult>();
                    if (result?.Success == true)
                    {
                        _currentUsername = username;
                        _isAuthenticated = true;
                        // In a real app, you'd store the JWT token
                    }
                    return result ?? new AuthResult { Success = false, Message = "Invalid response" };
                }
                else
                {
                    return new AuthResult { Success = false, Message = $"Login failed: {response.StatusCode}" };
                }
            }
            catch (Exception ex)
            {
                return new AuthResult { Success = false, Message = $"Login error: {ex.Message}" };
            }
        }

        public async Task<AuthResult> RegisterAsync(string username, string password, string confirmPassword)
        {
            try
            {
                var registerRequest = new { Username = username, Password = password, ConfirmPassword = confirmPassword };
                var response = await _httpClient.PostAsJsonAsync("/api/auth/register", registerRequest);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AuthResult>();
                    if (result?.Success == true)
                    {
                        _currentUsername = username;
                        _isAuthenticated = true;
                    }
                    return result ?? new AuthResult { Success = false, Message = "Invalid response" };
                }
                else
                {
                    return new AuthResult { Success = false, Message = $"Registration failed: {response.StatusCode}" };
                }
            }
            catch (Exception ex)
            {
                return new AuthResult { Success = false, Message = $"Registration error: {ex.Message}" };
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                await _httpClient.PostAsync("/api/auth/logout", null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logout error: {ex.Message}");
            }
            finally
            {
                _currentUsername = null;
                _currentUserId = 0;
                _isAuthenticated = false;
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            // In a real app, you'd check the JWT token validity
            return await Task.FromResult(_isAuthenticated);
        }

        public async Task<string?> GetCurrentUsernameAsync()
        {
            return await Task.FromResult(_currentUsername);
        }

        public async Task<int> GetCurrentUserIdAsync()
        {
            return await Task.FromResult(_currentUserId);
        }

        public async Task<CurrentUser?> GetCurrentUserAsync()
        {
            if (!_isAuthenticated || string.IsNullOrEmpty(_currentUsername))
                return null;

            return await Task.FromResult(new CurrentUser
            {
                Id = _currentUserId,
                Username = _currentUsername,
                IsAuthenticated = _isAuthenticated
            });
        }
    }

    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string username, string password);
        Task<AuthResult> RegisterAsync(string username, string password, string confirmPassword);
        Task LogoutAsync();
        Task<bool> IsAuthenticatedAsync();
        Task<string?> GetCurrentUsernameAsync();
        Task<int> GetCurrentUserIdAsync();
        Task<CurrentUser?> GetCurrentUserAsync();
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    public class CurrentUser
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool IsAuthenticated { get; set; }
    }
}
