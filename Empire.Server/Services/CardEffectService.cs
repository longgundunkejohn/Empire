using Empire.Shared.Models;
using Empire.Shared.Models.Enums;
using Empire.Server.Interfaces;

namespace Empire.Server.Services
{
    public class CardEffectService
    {
        private readonly ICardDatabaseService _cardService;
        private readonly ILogger<CardEffectService> _logger;

        public CardEffectService(ICardDatabaseService cardService, ILogger<CardEffectService> logger)
        {
            _cardService = cardService;
            _logger = logger;
        }

        /// <summary>
        /// Apply card effects when a card is played
        /// </summary>
        public async Task<CardEffectResult> ApplyCardEffect(GameState gameState, string playerId, int cardId, CardEffectContext context)
        {
            try
            {
                var card = await _cardService.GetCardAsync(cardId);
                if (card == null)
                {
                    return CardEffectResult.Failed("Card not found");
                }

                _logger.LogInformation("Applying effect for card {CardId} ({CardName}) by player {PlayerId}", 
                    cardId, card.Name, playerId);

                var result = new CardEffectResult { Success = true };

                // Apply effects based on card type and specific card
                switch (card.Type?.ToLowerInvariant())
                {
                    case "army":
                    case "unit":
                        result = await ApplyArmyCardEffect(gameState, playerId, card, context);
                        break;
                        
                    case "civic":
                    case "settlement":
                        result = await ApplyCivicCardEffect(gameState, playerId, card, context);
                        break;
                        
                    case "villager":
                        result = await ApplyVillagerEffect(gameState, playerId, card, context);
                        break;
                        
                    default:
                        result = await ApplyGenericCardEffect(gameState, playerId, card, context);
                        break;
                }

                if (result.Success)
                {
                    _logger.LogInformation("Successfully applied effect for card {CardId}", cardId);
                }
                else
                {
                    _logger.LogWarning("Failed to apply effect for card {CardId}: {Error}", cardId, result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying card effect for card {CardId}", cardId);
                return CardEffectResult.Failed($"Error applying card effect: {ex.Message}");
            }
        }

        private async Task<CardEffectResult> ApplyArmyCardEffect(GameState gameState, string playerId, Card card, CardEffectContext context)
        {
            var result = new CardEffectResult { Success = true };

            // Standard army card deployment
            if (context.Action == CardAction.Deploy)
            {
                // Calculate mana cost based on player tier
                int playerTier = gameState.PlayerTiers.GetValueOrDefault(playerId, 1);
                int manaCost = CalculateManaCost(card, playerTier);

                // Check if player can afford the card
                if (!CanAffordCard(gameState, playerId, manaCost))
                {
                    return CardEffectResult.Failed("Insufficient mana to deploy this card");
                }

                // Apply mana cost
                DeductMana(gameState, playerId, manaCost);

                // Move card from hand to heartland (exerted)
                if (gameState.PlayerArmyHands[playerId].Remove(card.CardId))
                {
                    gameState.PlayerHeartlands[playerId].Add(card.CardId);
                    result.AddEffect($"Deployed {card.Name} to heartland");
                }
            }

            // Apply specific card effects based on card name or ID
            await ApplySpecificCardEffects(gameState, playerId, card, context, result);

            return result;
        }

        private async Task<CardEffectResult> ApplyCivicCardEffect(GameState gameState, string playerId, Card card, CardEffectContext context)
        {
            var result = new CardEffectResult { Success = true };

            if (context.Action == CardAction.Settle && !string.IsNullOrEmpty(context.TerritoryId))
            {
                // Check if player is occupying the territory
                if (gameState.TerritoryOccupants.GetValueOrDefault(context.TerritoryId) != playerId)
                {
                    return CardEffectResult.Failed("You must be occupying the territory to settle it");
                }

                // Move card from hand to territory settlements
                if (gameState.PlayerCivicHands[playerId].Remove(card.CardId))
                {
                    if (!gameState.TerritorySettlements.ContainsKey(context.TerritoryId))
                        gameState.TerritorySettlements[context.TerritoryId] = new List<int>();
                    
                    gameState.TerritorySettlements[context.TerritoryId].Add(card.CardId);
                    
                    // Update player tier based on settlements
                    UpdatePlayerTier(gameState, playerId);
                    
                    result.AddEffect($"Settled {card.Name} in {context.TerritoryId}");
                }
            }

            // Apply specific settlement effects
            await ApplySpecificCardEffects(gameState, playerId, card, context, result);

            return result;
        }

        private async Task<CardEffectResult> ApplyVillagerEffect(GameState gameState, string playerId, Card card, CardEffectContext context)
        {
            var result = new CardEffectResult { Success = true };

            if (context.Action == CardAction.Play)
            {
                // Check once-per-round restriction
                if (gameState.PlayerActionsThisRound.GetValueOrDefault(playerId, new()).Contains("PlayVillager"))
                {
                    return CardEffectResult.Failed("You can only play one villager per round");
                }

                // Move card from hand to villagers
                if (gameState.PlayerCivicHands[playerId].Remove(card.CardId))
                {
                    gameState.PlayerVillagers[playerId].Add(card.CardId);
                    
                    // Track action for round restriction
                    if (!gameState.PlayerActionsThisRound.ContainsKey(playerId))
                        gameState.PlayerActionsThisRound[playerId] = new List<string>();
                    gameState.PlayerActionsThisRound[playerId].Add("PlayVillager");
                    
                    result.AddEffect($"Played villager {card.Name}");
                }
            }

            // Apply specific villager effects
            await ApplySpecificCardEffects(gameState, playerId, card, context, result);

            return result;
        }

        private async Task<CardEffectResult> ApplyGenericCardEffect(GameState gameState, string playerId, Card card, CardEffectContext context)
        {
            var result = new CardEffectResult { Success = true };

            // Apply any generic effects based on card description or special properties
            await ApplySpecificCardEffects(gameState, playerId, card, context, result);

            return result;
        }

        private async Task ApplySpecificCardEffects(GameState gameState, string playerId, Card card, CardEffectContext context, CardEffectResult result)
        {
            // This is where specific card effects would be implemented
            // For now, we'll implement a few example effects based on card names or descriptions

            switch (card.Name?.ToLowerInvariant())
            {
                case "militia":
                    // Example: Militia gives +1 morale when deployed
                    if (context.Action == CardAction.Deploy)
                    {
                        gameState.PlayerMorale[playerId] = Math.Min(25, gameState.PlayerMorale[playerId] + 1);
                        result.AddEffect("Gained 1 morale from Militia");
                    }
                    break;

                case "scout":
                    // Example: Scout allows drawing an extra card
                    if (context.Action == CardAction.Deploy)
                    {
                        result.AddTriggeredEffect(new TriggeredEffect
                        {
                            Type = EffectType.DrawCard,
                            PlayerId = playerId,
                            Description = "Scout allows drawing an extra army card"
                        });
                    }
                    break;

                case "fortress":
                    // Example: Fortress provides defensive bonus
                    if (context.Action == CardAction.Settle)
                    {
                        result.AddEffect("Fortress provides defensive bonus to territory");
                        // This would be tracked in territory modifiers
                    }
                    break;

                case "market":
                    // Example: Market provides economic bonus
                    if (context.Action == CardAction.Settle)
                    {
                        result.AddTriggeredEffect(new TriggeredEffect
                        {
                            Type = EffectType.DrawCard,
                            PlayerId = playerId,
                            CardType = "Civic",
                            Description = "Market allows drawing an extra civic card"
                        });
                    }
                    break;

                case "blacksmith":
                    // Example: Blacksmith reduces army card costs
                    if (context.Action == CardAction.Play)
                    {
                        result.AddEffect("Blacksmith reduces army card deployment costs");
                        // This would be tracked in player modifiers
                    }
                    break;
            }

            await Task.CompletedTask; // Placeholder for async operations
        }

        // Helper Methods

        private int CalculateManaCost(Card card, int playerTier)
        {
            // Basic mana cost calculation
            // Higher tier players pay less for cards
            int baseCost = card.ManaCost ?? 1;
            int tierDiscount = Math.Max(0, playerTier - 1);
            return Math.Max(1, baseCost - tierDiscount);
        }

        private bool CanAffordCard(GameState gameState, string playerId, int manaCost)
        {
            // For now, simplified mana system - players always have enough mana
            // TODO: Implement proper mana/resource system
            return true;
        }

        private void DeductMana(GameState gameState, string playerId, int manaCost)
        {
            // TODO: Implement mana deduction when mana system is added
        }

        private void UpdatePlayerTier(GameState gameState, string playerId)
        {
            // Calculate player tier based on number of settlements
            int totalSettlements = gameState.TerritorySettlements.Values
                .SelectMany(settlements => settlements)
                .Count(cardId => IsPlayerCard(gameState, playerId, cardId));

            int newTier = Math.Min(3, 1 + (totalSettlements / 2)); // Tier 1-3 based on settlements
            
            if (gameState.PlayerTiers.GetValueOrDefault(playerId, 1) != newTier)
            {
                gameState.PlayerTiers[playerId] = newTier;
                _logger.LogInformation("Player {PlayerId} advanced to tier {Tier}", playerId, newTier);
            }
        }

        private bool IsPlayerCard(GameState gameState, string playerId, int cardId)
        {
            // Check if the card belongs to the player
            return gameState.PlayerArmyHands[playerId].Contains(cardId) ||
                   gameState.PlayerCivicHands[playerId].Contains(cardId) ||
                   gameState.PlayerHeartlands[playerId].Contains(cardId) ||
                   gameState.PlayerVillagers[playerId].Contains(cardId);
        }
    }

    // Supporting classes

    public class CardEffectContext
    {
        public CardAction Action { get; set; }
        public string? TerritoryId { get; set; }
        public string? TargetPlayerId { get; set; }
        public int? TargetCardId { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public enum CardAction
    {
        Deploy,
        Play,
        Settle,
        Activate,
        Discard
    }

    public class CardEffectResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> Effects { get; set; } = new();
        public List<TriggeredEffect> TriggeredEffects { get; set; } = new();

        public static CardEffectResult Failed(string error)
        {
            return new CardEffectResult { Success = false, ErrorMessage = error };
        }

        public void AddEffect(string effect)
        {
            Effects.Add(effect);
        }

        public void AddTriggeredEffect(TriggeredEffect effect)
        {
            TriggeredEffects.Add(effect);
        }
    }

    public class TriggeredEffect
    {
        public EffectType Type { get; set; }
        public string PlayerId { get; set; } = string.Empty;
        public string? CardType { get; set; }
        public int? Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public enum EffectType
    {
        DrawCard,
        GainMorale,
        LoseMorale,
        ModifyStats,
        TriggerAbility
    }
}
