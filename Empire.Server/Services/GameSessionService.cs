using Empire.Server.Services;
using Empire.Shared.Models;
using Empire.Shared.Models.DTOs;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Empire.Server.Services
{
    public class GameSessionService
    {
        private readonly ILogger<GameSessionService> _logger;
        private readonly ICardDatabaseService _cardDatabase;
        private static readonly ConcurrentDictionary<string, GameState> _gameSessions = new();

        public GameState? GameState { get; private set; }

        public GameSessionService(ICardDatabaseService cardDatabase, ILogger<GameSessionService> logger)
        {
            _cardDatabase = cardDatabase;
            _logger = logger;
        }

        public async Task<string> CreateGameSession(string player1Id, List<RawDeckEntry> player1Deck)
        {
            var playerDeck = ConvertRawDeckToPlayerDeck(player1Id, player1Deck);
            var fullDeck = await HydrateDeckFromPlayerDeckAsync(playerDeck);

            var gameState = new GameState
            {
                GameId = Guid.NewGuid().ToString(),
                Player1 = player1Id,
                Player2 = null,
                InitiativeHolder = Random.Shared.Next(2) == 0 ? player1Id : null,
                PriorityPlayer = null,
                CurrentPhase = GamePhase.Strategy,
                GameBoardState = new GameBoard(),
                PlayerDecks = new Dictionary<string, List<Card>> { { player1Id, fullDeck } },
                PlayerHands = new Dictionary<string, List<int>> { { player1Id, new List<int>() } },
                PlayerBoard = new Dictionary<string, List<BoardCard>> { { player1Id, new List<BoardCard>() } },
                PlayerGraveyards = new Dictionary<string, List<int>> { { player1Id, new List<int>() } },
                PlayerLifeTotals = new Dictionary<string, int> { { player1Id, 25 } },
                MoveHistory = new List<GameMove>()
            };

            _logger.LogInformation("✅ Created game session {GameId} for player {Player} with {DeckCount} cards", 
                gameState.GameId, player1Id, fullDeck.Count);

            _gameSessions.TryAdd(gameState.GameId, gameState);
            return gameState.GameId;
        }

        public async Task<bool> JoinGame(string gameId, string playerId, List<Card> deck)
        {
            if (!_gameSessions.TryGetValue(gameId, out var gameState) || !string.IsNullOrEmpty(gameState.Player2))
                return false;

            // ✅ Ensure each card has its DeckType set
            foreach (var card in deck)
            {
                card.DeckType ??= InferDeckType(card);
            }

            gameState.Player2 = playerId;
            gameState.PlayerDecks[playerId] = deck;
            gameState.PlayerHands[playerId] = new List<int>();
            gameState.PlayerBoard[playerId] = new List<BoardCard>();
            gameState.PlayerGraveyards[playerId] = new List<int>();
            gameState.PlayerLifeTotals[playerId] = 25;

            if (string.IsNullOrEmpty(gameState.InitiativeHolder))
            {
                gameState.InitiativeHolder = Random.Shared.Next(2) == 0 ? gameState.Player1 : playerId;
            }

            _logger.LogInformation("✅ Player {Player} joined game {GameId}", playerId, gameId);
            return true;
        }

        // 🧠 Local helper in GameSessionService (copy this over from controller if needed)
        private string InferDeckType(Card card)
        {
            return card.Type?.ToLower() switch
            {
                "villager" => "Civic",
                "settlement" => "Civic",
                _ => "Military"
            };
        }

        public async Task<List<GameState>> ListOpenGames()
        {
            try
            {
                var openGames = _gameSessions.Values
                    .Where(gs => !string.IsNullOrEmpty(gs.Player1) && string.IsNullOrEmpty(gs.Player2))
                    .ToList();

                _logger.LogInformation("📋 Found {Count} open games", openGames.Count);
                return openGames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error listing open games");
                return new List<GameState>();
            }
        }

        public async Task<GameState?> GetGameState(string gameId)
        {
            _gameSessions.TryGetValue(gameId, out var state);
            GameState = state;
            return state;
        }

        public async Task SaveGameState(string gameId, GameState state)
        {
            _gameSessions.TryUpdate(gameId, state, _gameSessions.GetValueOrDefault(gameId));
            await Task.CompletedTask;
        }

        private PlayerDeck ConvertRawDeckToPlayerDeck(string playerName, List<RawDeckEntry> rawDeck)
        {
            var civicDeck = new List<int>();
            var militaryDeck = new List<int>();

            foreach (var entry in rawDeck)
            {
                var type = entry.DeckType?.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(type))
                {
                    type = DeckUtils.IsCivicCard(entry.CardId) ? "civic" : "military";
                }

                for (int i = 0; i < entry.Count; i++)
                {
                    if (type == "civic") civicDeck.Add(entry.CardId);
                    else if (type == "military") militaryDeck.Add(entry.CardId);
                    else _logger.LogWarning("Unknown deck type '{DeckType}' for card ID {CardId}", entry.DeckType, entry.CardId);
                }
            }

            _logger.LogInformation("Built deck for {Player} — Civic: {CivicCount}, Military: {MilitaryCount}",
                playerName, civicDeck.Count, militaryDeck.Count);

            return new PlayerDeck(playerName, civicDeck, militaryDeck);
        }

        private async Task<List<Card>> HydrateDeckFromPlayerDeckAsync(PlayerDeck deck)
        {
            var civicCards = await GetDeckCards(deck.CivicDeck, "Civic");
            var militaryCards = await GetDeckCards(deck.MilitaryDeck, "Military");

            _logger.LogInformation("✅ Hydrated {CivicCount} civic + {MilitaryCount} military = {TotalCount} total cards",
                civicCards.Count, militaryCards.Count, civicCards.Count + militaryCards.Count);

            return civicCards.Concat(militaryCards).ToList();
        }

        private async Task<List<Card>> GetDeckCards(List<int> cardIds, string deckType)
        {
            var allCardData = _cardDatabase.GetAllCards()
                .Where(cd => cardIds.Contains(cd.CardID))
                .ToDictionary(cd => cd.CardID, cd => cd);

            var result = new List<Card>();

            foreach (var id in cardIds)
            {
                if (allCardData.TryGetValue(id, out var cd))
                {
                    result.Add(new Card
                    {
                        CardId = cd.CardID,
                        Name = cd.Name,
                        CardText = cd.CardText,
                        Faction = cd.Faction,
                        Type = cd.CardType,
                        DeckType = deckType,
                        ImagePath = cd.ImageFileName ?? "images/Cards/placeholder.jpg",
                        IsExerted = false,
                        CurrentDamage = 0
                    });
                }
                else
                {
                    _logger.LogWarning("❌ Card ID {CardId} not found in database", id);
                }
            }

            return result;
        }

        public async Task<bool> ApplyMove(string gameId, GameMove move)
        {
            if (!_gameSessions.TryGetValue(gameId, out var gameState))
                return false;

            if (!ValidateMove(gameState, move)) 
                return false;

            await ProcessMoveAsync(gameState, move);
            return true;
        }

        private bool ValidateMove(GameState gameState, GameMove move) => true;
        
        private void ShuffleDeck(GameState gameState, string playerId)
        {
            if (!gameState.PlayerDecks.TryGetValue(playerId, out var cards))
            {
                _logger.LogWarning("Attempted to shuffle but no deck found for {PlayerId}", playerId);
                return;
            }

            var deck = new Deck(cards);
            deck.Shuffle();
            gameState.PlayerDecks[playerId] = deck.GetAllCards().ToList();

            _logger.LogInformation("🔀 Shuffled deck for {PlayerId}", playerId);
        }

        private async Task ProcessMoveAsync(GameState gameState, GameMove move)
        {
            var player = move.PlayerId;

            switch (move.MoveType)
            {
                case "ShuffleDeck":
                    ShuffleDeck(gameState, player);
                    _logger.LogInformation("[Move] Player {Player} shuffled their deck", player);
                    break;

                case "PlayCard":
                    if (!move.CardId.HasValue) break;

                    var cardId = move.CardId.Value;

                    if (!gameState.PlayerHands.TryGetValue(player, out var hand) || !hand.Contains(cardId))
                    {
                        _logger.LogWarning("[PlayCard] Player '{Player}' attempted to play invalid card ID {CardId}", player, cardId);
                        break;
                    }

                    // Remove card from hand
                    hand.Remove(cardId);

                    // Add to board
                    if (!gameState.PlayerBoard.ContainsKey(player))
                        gameState.PlayerBoard[player] = new List<BoardCard>();

                    gameState.PlayerBoard[player].Add(new BoardCard(cardId)
                    {
                        IsExerted = move.IsExerting ?? false,
                        Damage = 0
                    });

                    _logger.LogInformation("[PlayCard] Player '{Player}' played card ID {CardId}", player, cardId);
                    break;

                case "JoinGame":
                    if (!gameState.PlayerDecks.ContainsKey(player))
                    {
                        var joinMove = move as JoinGameMove;
                        if (joinMove?.PlayerDeck != null)
                        {
                            var fullDeck = await HydrateDeckFromIdsAsync(joinMove.PlayerDeck.CivicDeck, joinMove.PlayerDeck.MilitaryDeck);
                            gameState.PlayerDecks[player] = fullDeck;
                            gameState.PlayerHands[player] = new List<int>();
                            gameState.PlayerBoard[player] = new List<BoardCard>();
                            gameState.PlayerGraveyards[player] = new List<int>();
                            gameState.PlayerLifeTotals[player] = 25;

                            if (string.IsNullOrEmpty(gameState.InitiativeHolder))
                            {
                                gameState.InitiativeHolder = Random.Shared.Next(2) == 0 ? gameState.Player1 : player;
                            }

                            gameState.Player2 = player;
                        }
                    }
                    break;
            }

            gameState.MoveHistory.Add(move);
        }

        private async Task<List<Card>> HydrateDeckFromIdsAsync(List<int> civicIds, List<int> militaryIds)
        {
            var civicCards = await GetDeckCards(civicIds, "Civic");
            var militaryCards = await GetDeckCards(militaryIds, "Military");

            return civicCards.Concat(militaryCards).ToList();
        }

        private Deck GetDeckObject(GameState gameState, string playerId)
        {
            if (!gameState.PlayerDecks.TryGetValue(playerId, out var cards))
                throw new InvalidOperationException($"No deck found for player {playerId}");

            return new Deck(cards);
        }
    }
}
