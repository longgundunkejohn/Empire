using Microsoft.EntityFrameworkCore;
using Empire.Server.Data;
using Empire.Server.Models;
using System.Text.Json;
using Empire.Shared.Models;

namespace Empire.Server.Services
{
    public interface IUserService
    {
        Task<User> GetOrCreateUserAsync(string username);
        Task<List<UserDeck>> GetUserDecksAsync(string username);
        Task<UserDeck> SaveDeckAsync(string username, string deckName, List<int> armyCards, List<int> civicCards);
        Task<bool> DeleteDeckAsync(string username, string deckName);
        Task<Deck?> GetDeckAsync(string username, string deckName);
    }

    public class UserService : IUserService
    {
        private readonly EmpireDbContext _context;
        private readonly ICardDatabaseService _cardService;

        public UserService(EmpireDbContext context, ICardDatabaseService cardService)
        {
            _context = context;
            _cardService = cardService;
        }

        public async Task<User> GetOrCreateUserAsync(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            
            if (user == null)
            {
                user = new User { Username = username };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            
            return user;
        }

        public async Task<List<UserDeck>> GetUserDecksAsync(string username)
        {
            var user = await GetOrCreateUserAsync(username);
            return await _context.UserDecks
                .Where(d => d.UserId == user.Id)
                .OrderBy(d => d.DeckName)
                .ToListAsync();
        }

        public async Task<UserDeck> SaveDeckAsync(string username, string deckName, List<int> armyCards, List<int> civicCards)
        {
            var user = await GetOrCreateUserAsync(username);
            
            // Check if deck already exists
            var existingDeck = await _context.UserDecks
                .FirstOrDefaultAsync(d => d.UserId == user.Id && d.DeckName == deckName);

            if (existingDeck != null)
            {
                // Update existing deck
                existingDeck.ArmyCards = JsonSerializer.Serialize(armyCards);
                existingDeck.CivicCards = JsonSerializer.Serialize(civicCards);
                existingDeck.CreatedDate = DateTime.UtcNow;
            }
            else
            {
                // Create new deck
                existingDeck = new UserDeck
                {
                    UserId = user.Id,
                    DeckName = deckName,
                    ArmyCards = JsonSerializer.Serialize(armyCards),
                    CivicCards = JsonSerializer.Serialize(civicCards)
                };
                _context.UserDecks.Add(existingDeck);
            }

            await _context.SaveChangesAsync();
            return existingDeck;
        }

        public async Task<bool> DeleteDeckAsync(string username, string deckName)
        {
            var user = await GetOrCreateUserAsync(username);
            var deck = await _context.UserDecks
                .FirstOrDefaultAsync(d => d.UserId == user.Id && d.DeckName == deckName);

            if (deck != null)
            {
                _context.UserDecks.Remove(deck);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<Deck?> GetDeckAsync(string username, string deckName)
        {
            var user = await GetOrCreateUserAsync(username);
            var userDeck = await _context.UserDecks
                .FirstOrDefaultAsync(d => d.UserId == user.Id && d.DeckName == deckName);

            if (userDeck == null)
                return null;

            var armyCardIds = JsonSerializer.Deserialize<List<int>>(userDeck.ArmyCards) ?? new List<int>();
            var civicCardIds = JsonSerializer.Deserialize<List<int>>(userDeck.CivicCards) ?? new List<int>();

            // Convert card IDs to Card objects
            var allCardIds = armyCardIds.Concat(civicCardIds).ToList();
            var cards = await _cardService.GetDeckCards(allCardIds);

            return new Deck(cards);
        }
    }
}
