using Empire.Shared.Models;
using Empire.Shared.Models.DTOs;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;

namespace Empire.Server.Services
{
    public class GameSessionService
    {
        private readonly IMongoCollection<GameState> _gameCollection;

        public GameState? GameState { get; private set; }

        public GameSessionService(IMongoDatabase database)
        {
            _gameCollection = database.GetCollection<GameState>("GameSessions");
        }

        public async Task<string> CreateGameSession(string player1Id, List<RawDeckEntry> player1Deck)
        {
            var gameState = new GameState
            {
                GameId = Guid.NewGuid().ToString(),
                Player1 = player1Id,
                CurrentPhase = GamePhase.Strategy,
                InitiativeHolder = player1Id,
                PriorityPlayer = player1Id,
                GameBoardState = new GameBoard(),
                PlayerDecks = new Dictionary<string, List<Card>>(), // Changed to List<Card>
                PlayerHands = new Dictionary<string, List<int>>
                {
                    { player1Id, new List<int>() }
                },
                PlayerBoard = new Dictionary<string, List<BoardCard>>
                {
                    { player1Id, new List<BoardCard>() }
                },
                PlayerGraveyards = new Dictionary<string, List<int>>
                {
                    { player1Id, new List<int>() }
                },
                PlayerLifeTotals = new Dictionary<string, int>
                {
                    { player1Id, 25 }
                },
                MoveHistory = new List<GameMove>()
            };

            //  Store the deck in the PlayerDecks dictionary
            gameState.PlayerDecks[player1Id] = ConvertRawDeckToCardList(player1Deck); // Use the new method

            Console.WriteLine($"[CreateGame] Received name: '{player1Id}', deck: {player1Deck.Count} entries");

            await _gameCollection.InsertOneAsync(gameState);
            return gameState.GameId;
        }

        public async Task<GameState?> GetGameState(string gameId)
        {
            var state = await _gameCollection.Find(gs => gs.GameId == gameId).FirstOrDefaultAsync();
            GameState = state;
            return state;
        }

        public async Task<bool> ApplyMove(string gameId, GameMove move)
        {
            var gameState = await _gameCollection.Find(gs => gs.GameId == gameId).FirstOrDefaultAsync();
            if (gameState == null) return false;

            if (!ValidateMove(gameState, move)) return false;

            ProcessMove(gameState, move);

            await _gameCollection.ReplaceOneAsync(gs => gs.GameId == gameId, gameState);
            return true;
        }

        private bool ValidateMove(GameState gameState, GameMove move)
        {
            return true; // Simplified for now, custom rules can go here
        }

        private void ProcessMove(GameState gameState, GameMove move)
        {
            var player = move.PlayerId;

            switch (move.MoveType)
            {
                case "JoinGame":
                    if (!gameState.PlayerDecks.ContainsKey(player))
                    {
                        var joinMove = move as JoinGameMove;
                        if (joinMove?.PlayerDeck != null)
                        {
                            // gameState.PlayerDecks[player] = joinMove.PlayerDeck;  //  This was incorrect
                            //  We should be storing List<Card> here
                            gameState.PlayerDecks[player] = ConvertRawDeckToCardList(ConvertCardListToRawDeck(joinMove.PlayerDeck.Cards));
                            gameState.PlayerHands[player] = new List<int>();
                            gameState.PlayerBoard[player] = new List<BoardCard>();
                            gameState.PlayerGraveyards[player] = new List<int>();
                            gameState.PlayerLifeTotals[player] = 25;
                        }
                    }
                    break;

                    // (Rest of ProcessMove remains similar)
            }

            gameState.MoveHistory.Add(move);
        }

        public async Task<List<GameState>> ListOpenGames()
        {
            try
            {
                var filter = Builders<GameState>.Filter.Where(gs =>
                    !string.IsNullOrEmpty(gs.Player1) && string.IsNullOrEmpty(gs.Player2));

                var results = await _gameCollection.Find(filter).ToListAsync();

                Console.WriteLine($"[ListOpenGames] Found {results.Count} open games.");

                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ListOpenGames] Error: {ex.Message}");
                return new List<GameState>();
            }
        }

        private List<RawDeckEntry> ConvertCardListToRawDeck(List<Card> cards)
        {
            return cards
                .GroupBy(c => c.CardId)
                .Select(g => new RawDeckEntry
                {
                    CardId = g.Key,
                    Count = g.Count(),
                    DeckType = InferDeckType(g.First())
                })
                .ToList();
        }

        private string InferDeckType(Card card)
        {
            return card.Type.ToLower() switch
            {
                "villager" => "Civic",
                "settlement" => "Civic",
                _ => "Military"
            };
        }

        public async Task<bool> JoinGame(string gameId, string player2Id, List<Card> player2Deck)
        {
            var gameState = await _gameCollection.Find(gs => gs.GameId == gameId).FirstOrDefaultAsync();

            if (gameState == null || !string.IsNullOrEmpty(gameState.Player2))
                return false;

            gameState.Player2 = player2Id;
            var rawDeck = ConvertCardListToRawDeck(player2Deck);
            gameState.PlayerDecks[player2Id] = ConvertRawDeckToCardList(rawDeck); //  Use the new method
            gameState.PlayerHands[player2Id] = new List<int>();
            gameState.PlayerBoard[player2Id] = new List<BoardCard>();
            gameState.PlayerGraveyards[player2Id] = new List<int>();
            gameState.PlayerLifeTotals[player2Id] = 25;

            await _gameCollection.ReplaceOneAsync(gs => gs.GameId == gameId, gameState);
            return true;
        }

        private List<Card> ConvertRawDeckToCardList(List<RawDeckEntry> rawDeck)
        {
            var cards = new List<Card>();
            foreach (var entry in rawDeck)
            {
                for (int i = 0; i < entry.Count; i++)
                {
                    cards.Add(new Card { CardId = entry.CardId, Type = entry.DeckType }); //  Only CardId and Type are needed
                }
            }
            return cards;
        }
    }
}