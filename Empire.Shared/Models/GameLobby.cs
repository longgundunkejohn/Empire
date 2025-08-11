using System.ComponentModel.DataAnnotations;

namespace Empire.Shared.Models
{
    public class GameLobby
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public int HostUserId { get; set; }
        public string HostUsername { get; set; } = string.Empty;
        
        public int? Player1Id { get; set; }
        public string? Player1Username { get; set; }
        public string? Player1DeckName { get; set; }
        public bool Player1Ready { get; set; } = false;
        
        public int? Player2Id { get; set; }
        public string? Player2Username { get; set; }
        public string? Player2DeckName { get; set; }
        public bool Player2Ready { get; set; } = false;
        
        public List<SpectatorInfo> Spectators { get; set; } = new();
        public int MaxSpectators { get; set; } = 10;
        
        public LobbyStatus Status { get; set; } = LobbyStatus.WaitingForPlayers;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? StartedDate { get; set; }
        
        // Game settings
        public bool AllowSpectators { get; set; } = true;
        public bool RequireDeckValidation { get; set; } = true;
        public int TimeLimit { get; set; } = 30; // minutes per player
        
        // Helper properties
        public bool IsFull => Player1Id.HasValue && Player2Id.HasValue;
        public bool CanStart => IsFull && 
            (!RequireDeckValidation || (HasValidDeck(Player1DeckName) && HasValidDeck(Player2DeckName)));
        public int PlayerCount => (Player1Id.HasValue ? 1 : 0) + (Player2Id.HasValue ? 1 : 0);
        public int SpectatorCount => Spectators.Count;
        
        private bool HasValidDeck(string? deckName) => !string.IsNullOrEmpty(deckName);
        
        public bool IsHost(int userId) => HostUserId == userId;
        public bool IsPlayer(int userId) => Player1Id == userId || Player2Id == userId;
        public bool IsSpectator(int userId) => Spectators.Any(s => s.UserId == userId);
        public bool IsParticipant(int userId) => IsHost(userId) || IsPlayer(userId) || IsSpectator(userId);
        
        public PlayerSlot? GetPlayerSlot(int userId)
        {
            if (Player1Id == userId) return PlayerSlot.Player1;
            if (Player2Id == userId) return PlayerSlot.Player2;
            return null;
        }
    }
    
    public class SpectatorInfo
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
    }
    
    public enum LobbyStatus
    {
        WaitingForPlayers,
        ReadyToStart,
        InProgress,
        Completed,
        Cancelled
    }
    
    public enum PlayerSlot
    {
        Player1,
        Player2
    }
    
    // DTOs for API communication
    public class CreateLobbyRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;
        
        public bool AllowSpectators { get; set; } = true;
        public int MaxSpectators { get; set; } = 10;
        public bool RequireDeckValidation { get; set; } = true;
        public int TimeLimit { get; set; } = 30;
    }
    
    public class JoinLobbyRequest
    {
        [Required]
        public string LobbyId { get; set; } = string.Empty;
        
        public string? DeckName { get; set; }
        public PlayerSlot? PreferredSlot { get; set; }
    }
    
    public class LobbyListItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string HostUsername { get; set; } = string.Empty;
        public int PlayerCount { get; set; }
        public int SpectatorCount { get; set; }
        public LobbyStatus Status { get; set; }
        public bool AllowSpectators { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsFull { get; set; }
        public bool CanJoin { get; set; }
    }
}
