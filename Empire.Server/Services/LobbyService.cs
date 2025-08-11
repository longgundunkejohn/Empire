using Empire.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Empire.Server.Services
{
    public interface ILobbyService
    {
        Task<GameLobby> CreateLobbyAsync(CreateLobbyRequest request, int hostUserId, string hostUsername);
        Task<List<LobbyListItem>> GetActiveLobbiesAsync();
        Task<GameLobby?> GetLobbyAsync(string lobbyId);
        Task<bool> JoinLobbyAsync(string lobbyId, int userId, string username, string? deckName = null, PlayerSlot? preferredSlot = null);
        Task<bool> JoinAsSpectatorAsync(string lobbyId, int userId, string username);
        Task<bool> LeaveLobbyAsync(string lobbyId, int userId);
        Task<bool> SetPlayerReadyAsync(string lobbyId, int userId, bool ready);
        Task<bool> StartGameAsync(string lobbyId, int hostUserId);
        Task<bool> CancelLobbyAsync(string lobbyId, int hostUserId);
        Task<bool> UpdatePlayerDeckAsync(string lobbyId, int userId, string deckName);
        Task<List<string>> ValidateDeckAsync(string deckName, int userId);
        Task CleanupExpiredLobbiesAsync();
    }

    public class LobbyService : ILobbyService
    {
        private readonly ConcurrentDictionary<string, GameLobby> _lobbies = new();
        private readonly ILogger<LobbyService> _logger;
        private readonly IUserService _userService;
        private readonly GameStateService _gameStateService;
        private readonly ICardService _cardService;
        private readonly Timer _cleanupTimer;

        public LobbyService(ILogger<LobbyService> logger, IUserService userService, 
            GameStateService gameStateService, ICardService cardService)
        {
            _logger = logger;
            _userService = userService;
            _gameStateService = gameStateService;
            _cardService = cardService;
            
            // Cleanup expired lobbies every 5 minutes
            _cleanupTimer = new Timer(async _ => await CleanupExpiredLobbiesAsync(), 
                null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public async Task<GameLobby> CreateLobbyAsync(CreateLobbyRequest request, int hostUserId, string hostUsername)
        {
            var lobby = new GameLobby
            {
                Name = request.Name.Trim(),
                HostUserId = hostUserId,
                HostUsername = hostUsername,
                AllowSpectators = request.AllowSpectators,
                MaxSpectators = Math.Max(0, Math.Min(50, request.MaxSpectators)), // Limit between 0-50
                RequireDeckValidation = request.RequireDeckValidation,
                TimeLimit = Math.Max(5, Math.Min(120, request.TimeLimit)), // Limit between 5-120 minutes
                Status = LobbyStatus.WaitingForPlayers
            };

            // Host automatically joins as Player1
            lobby.Player1Id = hostUserId;
            lobby.Player1Username = hostUsername;

            _lobbies[lobby.Id] = lobby;
            
            _logger.LogInformation("Lobby {LobbyId} '{LobbyName}' created by user {UserId} ({Username})", 
                lobby.Id, lobby.Name, hostUserId, hostUsername);

            return lobby;
        }

        public async Task<List<LobbyListItem>> GetActiveLobbiesAsync()
        {
            var activeLobbies = _lobbies.Values
                .Where(l => l.Status == LobbyStatus.WaitingForPlayers || l.Status == LobbyStatus.ReadyToStart)
                .OrderByDescending(l => l.CreatedDate)
                .Select(l => new LobbyListItem
                {
                    Id = l.Id,
                    Name = l.Name,
                    HostUsername = l.HostUsername,
                    PlayerCount = l.PlayerCount,
                    SpectatorCount = l.SpectatorCount,
                    Status = l.Status,
                    AllowSpectators = l.AllowSpectators,
                    CreatedDate = l.CreatedDate,
                    IsFull = l.IsFull,
                    CanJoin = !l.IsFull || (l.AllowSpectators && l.SpectatorCount < l.MaxSpectators)
                })
                .ToList();

            return activeLobbies;
        }

        public async Task<GameLobby?> GetLobbyAsync(string lobbyId)
        {
            _lobbies.TryGetValue(lobbyId, out var lobby);
            return lobby;
        }

        public async Task<bool> JoinLobbyAsync(string lobbyId, int userId, string username, string? deckName = null, PlayerSlot? preferredSlot = null)
        {
            if (!_lobbies.TryGetValue(lobbyId, out var lobby))
            {
                _logger.LogWarning("Attempted to join non-existent lobby {LobbyId}", lobbyId);
                return false;
            }

            if (lobby.Status != LobbyStatus.WaitingForPlayers)
            {
                _logger.LogWarning("User {UserId} attempted to join lobby {LobbyId} with status {Status}", 
                    userId, lobbyId, lobby.Status);
                return false;
            }

            if (lobby.IsParticipant(userId))
            {
                _logger.LogWarning("User {UserId} attempted to join lobby {LobbyId} they're already in", 
                    userId, lobbyId);
                return false;
            }

            if (lobby.IsFull)
            {
                _logger.LogWarning("User {UserId} attempted to join full lobby {LobbyId}", userId, lobbyId);
                return false;
            }

            // Determine which slot to assign
            PlayerSlot targetSlot;
            if (preferredSlot.HasValue && IsSlotAvailable(lobby, preferredSlot.Value))
            {
                targetSlot = preferredSlot.Value;
            }
            else if (!lobby.Player1Id.HasValue)
            {
                targetSlot = PlayerSlot.Player1;
            }
            else if (!lobby.Player2Id.HasValue)
            {
                targetSlot = PlayerSlot.Player2;
            }
            else
            {
                return false; // No slots available
            }

            // Assign player to slot
            if (targetSlot == PlayerSlot.Player1)
            {
                lobby.Player1Id = userId;
                lobby.Player1Username = username;
                lobby.Player1DeckName = deckName;
            }
            else
            {
                lobby.Player2Id = userId;
                lobby.Player2Username = username;
                lobby.Player2DeckName = deckName;
            }

            // Update lobby status
            if (lobby.IsFull)
            {
                lobby.Status = lobby.CanStart ? LobbyStatus.ReadyToStart : LobbyStatus.WaitingForPlayers;
            }

            _logger.LogInformation("User {UserId} ({Username}) joined lobby {LobbyId} as {Slot}", 
                userId, username, lobbyId, targetSlot);

            return true;
        }

        public async Task<bool> JoinAsSpectatorAsync(string lobbyId, int userId, string username)
        {
            if (!_lobbies.TryGetValue(lobbyId, out var lobby))
            {
                return false;
            }

            if (!lobby.AllowSpectators)
            {
                _logger.LogWarning("User {UserId} attempted to spectate lobby {LobbyId} that doesn't allow spectators", 
                    userId, lobbyId);
                return false;
            }

            if (lobby.IsParticipant(userId))
            {
                _logger.LogWarning("User {UserId} attempted to spectate lobby {LobbyId} they're already in", 
                    userId, lobbyId);
                return false;
            }

            if (lobby.SpectatorCount >= lobby.MaxSpectators)
            {
                _logger.LogWarning("User {UserId} attempted to spectate full lobby {LobbyId}", userId, lobbyId);
                return false;
            }

            lobby.Spectators.Add(new SpectatorInfo
            {
                UserId = userId,
                Username = username,
                JoinedDate = DateTime.UtcNow
            });

            _logger.LogInformation("User {UserId} ({Username}) joined lobby {LobbyId} as spectator", 
                userId, username, lobbyId);

            return true;
        }

        public async Task<bool> LeaveLobbyAsync(string lobbyId, int userId)
        {
            if (!_lobbies.TryGetValue(lobbyId, out var lobby))
            {
                return false;
            }

            bool wasRemoved = false;

            // Remove from player slots
            if (lobby.Player1Id == userId)
            {
                lobby.Player1Id = null;
                lobby.Player1Username = null;
                lobby.Player1DeckName = null;
                wasRemoved = true;
            }
            else if (lobby.Player2Id == userId)
            {
                lobby.Player2Id = null;
                lobby.Player2Username = null;
                lobby.Player2DeckName = null;
                wasRemoved = true;
            }

            // Remove from spectators
            var spectator = lobby.Spectators.FirstOrDefault(s => s.UserId == userId);
            if (spectator != null)
            {
                lobby.Spectators.Remove(spectator);
                wasRemoved = true;
            }

            if (wasRemoved)
            {
                // If host left, cancel the lobby
                if (lobby.IsHost(userId))
                {
                    lobby.Status = LobbyStatus.Cancelled;
                    _logger.LogInformation("Lobby {LobbyId} cancelled because host {UserId} left", lobbyId, userId);
                }
                else
                {
                    // Update lobby status
                    lobby.Status = lobby.CanStart ? LobbyStatus.ReadyToStart : LobbyStatus.WaitingForPlayers;
                }

                _logger.LogInformation("User {UserId} left lobby {LobbyId}", userId, lobbyId);
            }

            return wasRemoved;
        }

        public async Task<bool> SetPlayerReadyAsync(string lobbyId, int userId, bool ready)
        {
            if (!_lobbies.TryGetValue(lobbyId, out var lobby))
            {
                return false;
            }

            if (!lobby.IsPlayer(userId))
            {
                _logger.LogWarning("Non-player user {UserId} attempted to set ready status in lobby {LobbyId}", userId, lobbyId);
                return false;
            }

            // Update the ready status for the appropriate player
            if (lobby.Player1Id == userId)
            {
                lobby.Player1Ready = ready;
            }
            else if (lobby.Player2Id == userId)
            {
                lobby.Player2Ready = ready;
            }

            _logger.LogInformation("User {UserId} set ready status to {Ready} in lobby {LobbyId}", userId, ready, lobbyId);

            // Update lobby status based on whether both players are ready and have decks
            var bothPlayersReady = lobby.Player1Ready && lobby.Player2Ready;
            var bothHaveDecks = lobby.CanStart;
            
            if (bothPlayersReady && bothHaveDecks)
            {
                lobby.Status = LobbyStatus.ReadyToStart;
            }
            else
            {
                lobby.Status = LobbyStatus.WaitingForPlayers;
            }

            return true;
        }

        public async Task<bool> StartGameAsync(string lobbyId, int hostUserId)
        {
            if (!_lobbies.TryGetValue(lobbyId, out var lobby))
            {
                return false;
            }

            if (!lobby.IsHost(hostUserId))
            {
                _logger.LogWarning("Non-host user {UserId} attempted to start lobby {LobbyId}", hostUserId, lobbyId);
                return false;
            }

            if (!lobby.CanStart)
            {
                _logger.LogWarning("Host {UserId} attempted to start lobby {LobbyId} that can't be started", 
                    hostUserId, lobbyId);
                return false;
            }

            try
            {
                // Initialize the game state
                var player1Id = lobby.Player1Id!.Value.ToString();
                var player2Id = lobby.Player2Id!.Value.ToString();
                
                await _gameStateService.InitializeEmpireGame(lobbyId, player1Id, player2Id);

                // Load player decks and set them up
                await SetupPlayerDecks(lobbyId, lobby.Player1Id.Value, lobby.Player1DeckName!);
                await SetupPlayerDecks(lobbyId, lobby.Player2Id.Value, lobby.Player2DeckName!);

                // Update lobby status
                lobby.Status = LobbyStatus.InProgress;
                lobby.StartedDate = DateTime.UtcNow;

                _logger.LogInformation("Lobby {LobbyId} started by host {UserId} - Game state initialized", lobbyId, hostUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize game state for lobby {LobbyId}", lobbyId);
                return false;
            }
        }

        private async Task SetupPlayerDecks(string gameId, int userId, string deckName)
        {
            try
            {
                // TODO: Load actual deck from database/service
                // For now, create mock decks to test the system
                var armyCards = CreateMockArmyDeck();
                var civicCards = CreateMockCivicDeck();

                await _gameStateService.SetupPlayerDecks(userId.ToString(), armyCards, civicCards);
                
                _logger.LogInformation("Set up decks for player {UserId} in game {GameId}", userId, gameId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to setup decks for player {UserId} in game {GameId}", userId, gameId);
                throw;
            }
        }

        private List<Card> CreateMockArmyDeck()
        {
            var cards = new List<Card>();
            for (int i = 1; i <= 30; i++)
            {
                cards.Add(new Card
                {
                    CardId = 1000 + i,
                    Name = $"Army Unit {i}",
                    Type = "Army",
                    Cost = 1 + (i % 5),
                    Attack = 1 + (i % 4),
                    Defense = 1 + (i % 3),
                    Description = $"Mock army unit {i}"
                });
            }
            return cards;
        }

        private List<Card> CreateMockCivicDeck()
        {
            var cards = new List<Card>();
            for (int i = 1; i <= 15; i++)
            {
                cards.Add(new Card
                {
                    CardId = 2000 + i,
                    Name = $"Civic Building {i}",
                    Type = "Civic",
                    Cost = 1 + (i % 3),
                    Description = $"Mock civic building {i}"
                });
            }
            return cards;
        }

        public async Task<bool> CancelLobbyAsync(string lobbyId, int hostUserId)
        {
            if (!_lobbies.TryGetValue(lobbyId, out var lobby))
            {
                return false;
            }

            if (!lobby.IsHost(hostUserId))
            {
                _logger.LogWarning("Non-host user {UserId} attempted to cancel lobby {LobbyId}", hostUserId, lobbyId);
                return false;
            }

            lobby.Status = LobbyStatus.Cancelled;
            
            _logger.LogInformation("Lobby {LobbyId} cancelled by host {UserId}", lobbyId, hostUserId);

            return true;
        }

        public async Task<bool> UpdatePlayerDeckAsync(string lobbyId, int userId, string deckName)
        {
            if (!_lobbies.TryGetValue(lobbyId, out var lobby))
            {
                return false;
            }

            if (!lobby.IsPlayer(userId))
            {
                return false;
            }

            if (lobby.Player1Id == userId)
            {
                lobby.Player1DeckName = deckName;
            }
            else if (lobby.Player2Id == userId)
            {
                lobby.Player2DeckName = deckName;
            }

            // Update lobby status based on deck validation
            lobby.Status = lobby.CanStart ? LobbyStatus.ReadyToStart : LobbyStatus.WaitingForPlayers;

            _logger.LogInformation("User {UserId} updated deck to '{DeckName}' in lobby {LobbyId}", 
                userId, deckName, lobbyId);

            return true;
        }

        public async Task<List<string>> ValidateDeckAsync(string deckName, int userId)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(deckName))
            {
                errors.Add("Deck name is required");
                return errors;
            }

            // TODO: Implement actual deck validation
            // - Check if deck exists for user
            // - Validate 30 Army + 15 Civic cards
            // - Check card legality
            // - Validate deck construction rules

            // For now, just check if deck name is provided
            if (deckName.Length < 3)
            {
                errors.Add("Deck name must be at least 3 characters");
            }

            return errors;
        }

        public async Task CleanupExpiredLobbiesAsync()
        {
            var expiredTime = DateTime.UtcNow.AddHours(-2); // Remove lobbies older than 2 hours
            var expiredLobbies = _lobbies.Values
                .Where(l => l.CreatedDate < expiredTime && 
                           (l.Status == LobbyStatus.Cancelled || l.Status == LobbyStatus.Completed))
                .ToList();

            foreach (var lobby in expiredLobbies)
            {
                _lobbies.TryRemove(lobby.Id, out _);
                _logger.LogInformation("Cleaned up expired lobby {LobbyId}", lobby.Id);
            }

            if (expiredLobbies.Any())
            {
                _logger.LogInformation("Cleaned up {Count} expired lobbies", expiredLobbies.Count);
            }
        }

        private bool IsSlotAvailable(GameLobby lobby, PlayerSlot slot)
        {
            return slot switch
            {
                PlayerSlot.Player1 => !lobby.Player1Id.HasValue,
                PlayerSlot.Player2 => !lobby.Player2Id.HasValue,
                _ => false
            };
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }
    }
}
