using System.Text.Json.Serialization;

namespace Empire.Shared.Models.DTOs
{
    /// <summary>
    /// DTO for displaying game previews in the lobby
    /// Used for showing available games that players can join
    /// </summary>
    public class GamePreview
    {
        public string GameId { get; set; } = string.Empty;
        public string GameName { get; set; } = string.Empty;
        public string HostPlayerName { get; set; } = string.Empty;
        public string HostPlayer { get; set; } = string.Empty; // Legacy property for backward compatibility
        public int PlayerCount { get; set; } = 0;
        public int MaxPlayers { get; set; } = 2; // Empire is 2-player
        public string Status { get; set; } = "Waiting"; // Waiting, InProgress, Completed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsPasswordProtected { get; set; } = false;
        public string GameMode { get; set; } = "Empire"; // Future: could support different modes
        public bool IsJoinable { get; set; } = true; // Legacy property for backward compatibility
        
        [JsonIgnore]
        public bool IsFull => PlayerCount >= MaxPlayers;
        
        [JsonIgnore]
        public bool CanJoin => Status == "Waiting" && !IsFull;
        
        [JsonIgnore]
        public string DisplayStatus => Status switch
        {
            "Waiting" => $"Waiting ({PlayerCount}/{MaxPlayers})",
            "InProgress" => "In Progress",
            "Completed" => "Completed",
            _ => Status
        };
        
        [JsonIgnore]
        public string StatusColor => Status switch
        {
            "Waiting" => IsFull ? "orange" : "green",
            "InProgress" => "blue",
            "Completed" => "gray",
            _ => "black"
        };
    }
}
