using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Empire.Client.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private string? _currentUsername;
        private int _currentUserId;
        private bool _isAuthenticated;
        private string? _currentToken;

        public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
        }

        public async Task<AuthResult> LoginAsync(string username, string password)
        {
            try
            {
                var loginRequest = new { Username = username, Password = password };
                var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var serverResponse = JsonSerializer.Deserialize<ServerAuthResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (serverResponse != null && !string.IsNullOrEmpty(serverResponse.Token))
                    {
                        // Store authentication data
                        _currentUsername = serverResponse.User?.Username ?? username;
                        _currentUserId = serverResponse.User?.Id ?? 0;
                        _currentToken = serverResponse.Token;
                        _isAuthenticated = true;

                        // Store token in localStorage
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", _currentToken);
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "username", _currentUsername);
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userId", _currentUserId.ToString());

                        // Set authorization header for future requests
                        _httpClient.DefaultRequestHeaders.Authorization = 
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _currentToken);

                        return new AuthResult 
                        { 
                            Success = true, 
                            Message = serverResponse.Message ?? "Login successful",
                            Token = _currentToken
                        };
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return new AuthResult 
                { 
                    Success = false, 
                    Message = errorResponse?.Message ?? $"Login failed: {response.StatusCode}" 
                };
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
                    var content = await response.Content.ReadAsStringAsync();
                    var serverResponse = JsonSerializer.Deserialize<ServerAuthResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (serverResponse != null && !string.IsNullOrEmpty(serverResponse.Token))
                    {
                        // Store authentication data
                        _currentUsername = serverResponse.User?.Username ?? username;
                        _currentUserId = serverResponse.User?.Id ?? 0;
                        _currentToken = serverResponse.Token;
                        _isAuthenticated = true;

                        // Store token in localStorage
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", _currentToken);
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "username", _currentUsername);
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userId", _currentUserId.ToString());

                        // Set authorization header for future requests
                        _httpClient.DefaultRequestHeaders.Authorization = 
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _currentToken);

                        return new AuthResult 
                        { 
                            Success = true, 
                            Message = serverResponse.Message ?? "Registration successful",
                            Token = _currentToken
                        };
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return new AuthResult 
                { 
                    Success = false, 
                    Message = errorResponse?.Message ?? $"Registration failed: {response.StatusCode}" 
                };
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
                // Call logout endpoint if token exists
                if (!string.IsNullOrEmpty(_currentToken))
                {
                    await _httpClient.PostAsync("/api/auth/logout", null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logout error: {ex.Message}");
            }
            finally
            {
                // Clear all authentication data
                _currentUsername = null;
                _currentUserId = 0;
                _currentToken = null;
                _isAuthenticated = false;

                // Clear localStorage
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "username");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "userId");

                // Clear authorization header
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            if (_isAuthenticated && !string.IsNullOrEmpty(_currentToken))
            {
                return true;
            }

            // Try to restore from localStorage
            try
            {
                var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
                var username = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "username");
                var userIdStr = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "userId");

                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(username))
                {
                    _currentToken = token;
                    _currentUsername = username;
                    _currentUserId = int.TryParse(userIdStr, out var userId) ? userId : 0;
                    _isAuthenticated = true;

                    // Set authorization header
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _currentToken);

                    // Optionally validate token with server
                    var isValid = await ValidateTokenWithServer();
                    if (!isValid)
                    {
                        await LogoutAsync();
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking authentication: {ex.Message}");
            }

            return false;
        }

        public async Task<string?> GetCurrentUsernameAsync()
        {
            if (!await IsAuthenticatedAsync())
                return null;
                
            return _currentUsername;
        }

        public async Task<int> GetCurrentUserIdAsync()
        {
            if (!await IsAuthenticatedAsync())
                return 0;
                
            return _currentUserId;
        }

        public async Task<CurrentUser?> GetCurrentUserAsync()
        {
            if (!await IsAuthenticatedAsync())
                return null;

            return new CurrentUser
            {
                Id = _currentUserId,
                Username = _currentUsername ?? "",
                IsAuthenticated = _isAuthenticated
            };
        }

        private async Task<bool> ValidateTokenWithServer()
        {
            try
            {
                var response = await _httpClient.PostAsync("/api/auth/validate-token", null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
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

    public class ServerAuthResponse
    {
        public string? Message { get; set; }
        public ServerUser? User { get; set; }
        public string? Token { get; set; }
    }

    public class ServerUser
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ErrorResponse
    {
        public string? Message { get; set; }
    }
}
