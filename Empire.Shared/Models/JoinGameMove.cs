namespace Empire.Shared.Models
{
    public class JoinGameMove
    {
        public string PlayerId { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public string GameId { get; set; } = string.Empty;
        public PlayerDeck? PlayerDeck { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
