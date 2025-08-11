using Empire.Shared.Models;
using Empire.Shared.Models.DTOs;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Empire.Server.Services
{
    public class DeckService
    {
        private static readonly ConcurrentDictionary<string, PlayerDeck> _playerDecks = new();
        private static readonly ConcurrentDictionary<string, List<RawDeckEntry>> _rawDeckEntries = new();
        private readonly ILogger<DeckService> _logger;

        public DeckService(ILogger<DeckService> logger)
        {
            _logger = logger;
        }

        // Save final deck for the player
        public async Task SaveDeckAsync(PlayerDeck deck)
        {
            _playerDecks.AddOrUpdate(deck.PlayerName, deck, (key, oldValue) => deck);

            _logger.LogInformation("✅ Saved deck for player {PlayerName}. Civic: {CivicCount}, Military: {MilitaryCount}",
                deck.PlayerName,
                deck.CivicDeck?.Count ?? 0,
                deck.MilitaryDeck?.Count ?? 0);

            await Task.CompletedTask;
        }

        // Get the final deck for the player
        public async Task<PlayerDeck?> GetDeckAsync(string playerName, string deckId)
        {
            _playerDecks.TryGetValue(playerName, out var deck);
            return await Task.FromResult(deck);
        }

        // Get deck by player name only
        public async Task<PlayerDeck?> GetDeckAsync(string playerName)
        {
            _playerDecks.TryGetValue(playerName, out var deck);
            return await Task.FromResult(deck);
        }

        // Check if the player has a deck
        public async Task<bool> HasDeckAsync(string playerName)
        {
            var hasDeck = _playerDecks.ContainsKey(playerName);
            return await Task.FromResult(hasDeck);
        }

        // Delete the deck of a player
        public async Task DeleteDeckAsync(string playerName)
        {
            _playerDecks.TryRemove(playerName, out _);
            _rawDeckEntries.TryRemove(playerName, out _);
            _logger.LogInformation("🗑 Deleted deck for player {Player}.", playerName);
            await Task.CompletedTask;
        }

        // Get all decks
        public async Task<List<PlayerDeck>> GetAllDecksAsync()
        {
            var allDecks = _playerDecks.Values.ToList();
            return await Task.FromResult(allDecks);
        }

        // Get all player names who have decks
        public async Task<List<string>> GetAllPlayerNamesAsync()
        {
            var playerNames = _playerDecks.Keys.ToList();
            return await Task.FromResult(playerNames);
        }

        // Save raw deck entries (for CSV uploads)
        public async Task SaveRawDeckEntriesAsync(string playerName, List<RawDeckEntry> rawEntries)
        {
            _rawDeckEntries.AddOrUpdate(playerName, rawEntries, (key, oldValue) => rawEntries);
            
            _logger.LogInformation("✅ Saved {Count} raw deck entries for player {Player}", 
                rawEntries.Count, playerName);
            
            await Task.CompletedTask;
        }

        // Get raw deck entries for a player
        public async Task<List<RawDeckEntry>> GetRawDeckEntriesAsync(string playerName)
        {
            _rawDeckEntries.TryGetValue(playerName, out var entries);
            return await Task.FromResult(entries ?? new List<RawDeckEntry>());
        }

        // Convert raw deck entries to PlayerDeck
        public async Task<PlayerDeck> ConvertRawDeckToPlayerDeck(string playerName, List<RawDeckEntry> rawEntries)
        {
            var civicDeck = new List<int>();
            var militaryDeck = new List<int>();

            foreach (var entry in rawEntries)
            {
                var type = entry.DeckType?.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(type))
                {
                    // Infer deck type based on card ID ranges or other logic
                    type = DeckUtils.IsCivicCard(entry.CardId) ? "civic" : "military";
                }

                for (int i = 0; i < entry.Count; i++)
                {
                    if (type == "civic")
                        civicDeck.Add(entry.CardId);
                    else if (type == "military")
                        militaryDeck.Add(entry.CardId);
                    else
                        _logger.LogWarning("Unknown deck type '{DeckType}' for card ID {CardId}", entry.DeckType, entry.CardId);
                }
            }

            var playerDeck = new PlayerDeck(playerName, civicDeck, militaryDeck);
            
            _logger.LogInformation("Converted raw deck for {Player} — Civic: {CivicCount}, Military: {MilitaryCount}",
                playerName, civicDeck.Count, militaryDeck.Count);

            return await Task.FromResult(playerDeck);
        }

        // Get deck statistics
        public async Task<DeckStatistics> GetDeckStatisticsAsync(string playerName)
        {
            if (!_playerDecks.TryGetValue(playerName, out var deck))
            {
                return await Task.FromResult(new DeckStatistics
                {
                    PlayerName = playerName,
                    HasDeck = false
                });
            }

            return await Task.FromResult(new DeckStatistics
            {
                PlayerName = playerName,
                HasDeck = true,
                CivicCardCount = deck.CivicDeck?.Count ?? 0,
                MilitaryCardCount = deck.MilitaryDeck?.Count ?? 0,
                TotalCardCount = (deck.CivicDeck?.Count ?? 0) + (deck.MilitaryDeck?.Count ?? 0)
            });
        }
    }

    // Helper class for deck statistics
    public class DeckStatistics
    {
        public string PlayerName { get; set; } = string.Empty;
        public bool HasDeck { get; set; }
        public int CivicCardCount { get; set; }
        public int MilitaryCardCount { get; set; }
        public int TotalCardCount { get; set; }
    }
}
