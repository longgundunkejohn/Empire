namespace Empire.Shared.DTOs
{
    public class GamePreview
    {
        public string GameId { get; set; } = string.Empty;
        public string HostPlayer { get; set; } = string.Empty;
        public bool IsJoinable { get; set; }
    }
}
