using Empire.Shared.Models;

namespace Empire.Server.Models
{
    public class UserDeck : Empire.Shared.Models.UserDeck
    {
        // Navigation property (server-only)
        public User User { get; set; } = null!;
    }
}
