using Empire.Shared.Models;
using Empire.Shared.Models.Enums;
using Empire.Server.Services;
using System.Collections.Generic;
using System.Linq;

namespace Empire.Server.Services
{
    public class GameStateService
    {
        private readonly ICardService _cardService;
        private readonly BoardService _boardService;
        private readonly MongoDbService _mongoDbService;

        public GameState GameState { get; private set; }

        public GameStateService(ICardService cardService, BoardService boardService, MongoDbService mongoDbService)
        {
            _cardService = cardService;
            _boardService = boardService;
            _mongoDbService = mongoDbService;
            GameState = new GameState();
        }

        // Empire Game Initialization
        
        public async Task InitializeEmpireGame(string gameId, string player1Id, string player2Id)
        {
            GameState.GameId = gameId;
            GameState.Player1 = player1Id;
            GameState.Player2 = player2Id;
            GameState.CurrentPhase = GamePhase.Strategy;
            GameState.CurrentRound = 1;
            GameState.InitiativeHolder = player1Id; // Player 1 starts with initiative
            
            // Initialize both players
            GameState.InitializePlayer(player1Id);
            GameState.InitializePlayer(player2Id);
            
            await SaveGameState();
        }

        public async Task SetupPlayerDecks(string playerId, List<Card> armyDeck, List<Card> civicDeck)
        {
            // Validate deck sizes
            if (armyDeck.Count != 30)
                throw new ArgumentException("Army deck must contain exactly 30 cards");
            if (civicDeck.Count != 15)
                throw new ArgumentException("Civic deck must contain exactly 15 cards");
            
            // Shuffle decks
            ShuffleDeck(armyDeck);
            ShuffleDeck(civicDeck);
            
            // Set up decks in game state
            GameState.PlayerArmyDecks[playerId] = armyDeck;
            GameState.PlayerCivicDecks[playerId] = civicDeck;
            
            // Draw starting hand: 4 Army + 3 Civic
            DrawStartingHand(playerId);
            
            await SaveGameState();
        }

        private void DrawStartingHand(string playerId)
        {
            // Draw 4 Army cards
            for (int i = 0; i < 4 && GameState.PlayerArmyDecks[playerId].Count > 0; i++)
            {
                var card = GameState.PlayerArmyDecks[playerId][0];
                GameState.PlayerArmyDecks[playerId].RemoveAt(0);
                GameState.PlayerArmyHands[playerId].Add(card.CardId);
            }
            
            // Draw 3 Civic cards
            for (int i = 0; i < 3 && GameState.PlayerCivicDecks[playerId].Count > 0; i++)
            {
                var card = GameState.PlayerCivicDecks[playerId][0];
                GameState.PlayerCivicDecks[playerId].RemoveAt(0);
                GameState.PlayerCivicHands[playerId].Add(card.CardId);
            }
        }

        // Empire Actions
        
        public async Task<bool> DeployArmyCard(string playerId, int cardId, int manaCost)
        {
            if (!CanPlayerTakeAction(playerId)) return false;
            
            // Remove from army hand
            if (!GameState.PlayerArmyHands[playerId].Remove(cardId)) return false;
            
            // Add to heartland (exerted)
            GameState.PlayerHeartlands[playerId].Add(cardId);
            
            // Deduct mana (simplified for now)
            // TODO: Implement proper mana system
            
            // Pass initiative
            PassInitiative(playerId);
            
            await SaveGameState();
            return true;
        }

        public async Task<bool> PlayVillager(string playerId, int cardId)
        {
            if (!CanPlayerTakeAction(playerId)) return false;
            
            // Check once-per-round restriction
            // TODO: Implement round restriction tracking
            
            // Remove from civic hand
            if (!GameState.PlayerCivicHands[playerId].Remove(cardId)) return false;
            
            // Add to villagers
            GameState.PlayerVillagers[playerId].Add(cardId);
            
            // Pass initiative
            PassInitiative(playerId);
            
            await SaveGameState();
            return true;
        }

        public async Task<bool> SettleTerritory(string playerId, int cardId, string territoryId)
        {
            if (!CanPlayerTakeAction(playerId)) return false;
            
            // Check if player is occupying the territory
            if (GameState.TerritoryOccupants[territoryId] != playerId) return false;
            
            // Remove from civic hand
            if (!GameState.PlayerCivicHands[playerId].Remove(cardId)) return false;
            
            // Add to territory settlements
            GameState.TerritorySettlements[territoryId].Add(cardId);
            
            // Update player tier
            GameState.UpdatePlayerTier(playerId);
            
            // Pass initiative
            PassInitiative(playerId);
            
            await SaveGameState();
            return true;
        }

        public async Task<bool> CommitUnits(string playerId, Dictionary<int, string> unitCommitments)
        {
            if (!CanPlayerTakeAction(playerId)) return false;
            
            // Move units from heartland to territories
            foreach (var commitment in unitCommitments)
            {
                int cardId = commitment.Key;
                string territoryId = commitment.Value;
                
                if (GameState.PlayerHeartlands[playerId].Remove(cardId))
                {
                    GameState.TerritoryAdvancingUnits[territoryId].Add(cardId);
                }
            }
            
            // Pass initiative
            PassInitiative(playerId);
            
            await SaveGameState();
            return true;
        }

        // Initiative System
        
        public async Task<bool> PassInitiative(string playerId)
        {
            if (!CanPlayerTakeAction(playerId)) return false;
            
            string opponentId = GetOpponentId(playerId);
            
            // Check if both players have passed
            if (GameState.LastPlayerToPass == opponentId)
            {
                // Both players passed, advance phase
                await AdvancePhase();
            }
            else
            {
                // Pass initiative to opponent
                GameState.InitiativeHolder = opponentId;
                GameState.LastPlayerToPass = playerId;
            }
            
            await SaveGameState();
            return true;
        }

        private async Task AdvancePhase()
        {
            switch (GameState.CurrentPhase)
            {
                case GamePhase.Strategy:
                    GameState.CurrentPhase = GamePhase.Battle;
                    await ProcessBattlePhase();
                    break;
                    
                case GamePhase.Battle:
                    GameState.CurrentPhase = GamePhase.Replenishment;
                    await ProcessReplenishmentPhase();
                    break;
                    
                case GamePhase.Replenishment:
                    GameState.CurrentPhase = GamePhase.Strategy;
                    GameState.CurrentRound++;
                    await ProcessNewRound();
                    break;
            }
            
            // Reset pass tracking
            GameState.LastPlayerToPass = null;
            GameState.WaitingForBothPlayersToPass = false;
            
            // Initiative goes to the player who didn't have it
            string currentHolder = GameState.InitiativeHolder ?? GameState.Player1;
            GameState.InitiativeHolder = GetOpponentId(currentHolder);
        }

        private async Task ProcessBattlePhase()
        {
            // Process combat in all territories
            foreach (string territoryId in new[] { "territory-1", "territory-2", "territory-3" })
            {
                await ProcessTerritoryCombat(territoryId);
            }
        }

        private async Task ProcessTerritoryCombat(string territoryId)
        {
            var advancingUnits = GameState.TerritoryAdvancingUnits[territoryId];
            var occupyingUnits = new List<int>(); // TODO: Get occupying units
            
            // Simplified combat - just move advancing units to occupying
            // TODO: Implement proper combat resolution
            foreach (int cardId in advancingUnits.ToList())
            {
                advancingUnits.Remove(cardId);
                // Move to occupying or determine winner
            }
        }

        private async Task ProcessReplenishmentPhase()
        {
            // Unexert all cards
            // TODO: Implement card exertion tracking
            
            // Draw cards for each player
            foreach (string playerId in new[] { GameState.Player1, GameState.Player2 })
            {
                if (playerId == null) continue;
                
                // Player chooses: 1 Army OR 2 Civic cards
                // For now, default to 1 Army card
                DrawArmyCard(playerId);
            }
        }

        private async Task ProcessNewRound()
        {
            // Reset round restrictions
            // TODO: Implement round restriction tracking
            
            // Pass initiative tracker
            // TODO: Implement initiative tracker mechanics
        }

        // Card Drawing
        
        public async Task DrawArmyCard(string playerId)
        {
            if (GameState.PlayerArmyDecks[playerId].Count > 0)
            {
                var card = GameState.PlayerArmyDecks[playerId][0];
                GameState.PlayerArmyDecks[playerId].RemoveAt(0);
                GameState.PlayerArmyHands[playerId].Add(card.CardId);
                await SaveGameState();
            }
        }

        public async Task DrawCivicCards(string playerId, int count = 2)
        {
            for (int i = 0; i < count && GameState.PlayerCivicDecks[playerId].Count > 0; i++)
            {
                var card = GameState.PlayerCivicDecks[playerId][0];
                GameState.PlayerCivicDecks[playerId].RemoveAt(0);
                GameState.PlayerCivicHands[playerId].Add(card.CardId);
            }
            await SaveGameState();
        }

        // Helper Methods
        
        public bool CanPlayerTakeAction(string playerId)
        {
            return GameState.InitiativeHolder == playerId;
        }

        public string GetOpponentId(string playerId)
        {
            return playerId == GameState.Player1 ? 
                GameState.Player2 ?? "" : 
                GameState.Player1;
        }

        public async Task UpdateMorale(string playerId, int damage)
        {
            GameState.PlayerMorale[playerId] = Math.Max(0, GameState.PlayerMorale[playerId] - damage);
            await SaveGameState();
        }

        public bool IsGameOver()
        {
            return GameState.PlayerMorale.Values.Any(morale => morale <= 0);
        }

        public string? GetWinner()
        {
            if (!IsGameOver()) return null;
            
            var loser = GameState.PlayerMorale.FirstOrDefault(kvp => kvp.Value <= 0);
            return GetOpponentId(loser.Key);
        }

        // Persistence
        
        public async Task SaveGameState()
        {
            try
            {
                var collection = _mongoDbService.GameDatabase.GetCollection<GameState>("GameStates");
                await collection.ReplaceOneAsync(
                    gs => gs.GameId == GameState.GameId,
                    GameState,
                    new MongoDB.Driver.ReplaceOptions { IsUpsert = true }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving game state: {ex.Message}");
            }
        }

        public async Task LoadGameState(string gameId)
        {
            try
            {
                var collection = _mongoDbService.GameDatabase.GetCollection<GameState>("GameStates");
                var loadedState = await collection.Find(gs => gs.GameId == gameId).FirstOrDefaultAsync();
                if (loadedState != null)
                {
                    GameState = loadedState;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading game state: {ex.Message}");
            }
        }

        // Utility Methods
        
        private void ShuffleDeck(List<Card> deck)
        {
            var random = new Random();
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
            }
        }

        // Legacy methods (keeping for compatibility)
        
        public void InitializeGame(string playerId, List<int> civicDeck, List<int> militaryDeck)
        {
            // Legacy method - convert to new Empire initialization
            var cards = civicDeck.Select(id => new Card { CardId = id, Type = "Civic" })
                .Concat(militaryDeck.Select(id => new Card { CardId = id, Type = "Military" }))
                .ToList();

            _boardService.InitializePlayer(playerId, cards);
            GameState.PlayerLifeTotals[playerId] = 25;
        }

        public void DrawCard(string playerId, bool isCivic)
        {
            if (isCivic)
            {
                _ = DrawCivicCards(playerId, 1);
            }
            else
            {
                _ = DrawArmyCard(playerId);
            }
        }

        public void PlayCard(string playerId, int cardId)
        {
            _boardService.MoveToBoard(playerId, cardId);
        }
    }
}
