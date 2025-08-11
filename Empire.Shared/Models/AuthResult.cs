namespace Empire.Shared.Models
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public User? User { get; set; }
        public string? Token { get; set; }

        public static AuthResult CreateSuccess(User user, string token)
        {
            return new AuthResult
            {
                Success = true,
                User = user,
                Token = token
            };
        }

        public static AuthResult CreateFailure(string errorMessage)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
