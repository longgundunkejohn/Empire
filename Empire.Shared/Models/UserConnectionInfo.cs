namespace Empire.Shared.Models
{
    public class UserConnectionInfo
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
    }
}
