using System;

namespace Empire.Shared.Models
{
    public class Card
    {
        public int CardId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CardText { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Faction { get; set; } = string.Empty;
        public string DeckType { get; set; } = "Army"; // Army or Civic
        public string? ImagePath { get; set; }
        
        // Empire-specific properties
        public bool IsExerted { get; set; } = false;
        public int CurrentDamage { get; set; } = 0;
        public CardPosition Position { get; set; } = CardPosition.ArmyHand;
        public string? TerritoryId { get; set; } // Which territory if in play (territory-1, territory-2, territory-3)
        public bool IsOccupying { get; set; } = false; // true = occupying, false = advancing
        
        // Card stats from CardData
        public int Cost { get; set; } = 0;
        public int Attack { get; set; } = 0;
        public int Defense { get; set; } = 0;
        public string Tier { get; set; } = "I"; // I, II, III, IV
        public bool IsUnique { get; set; } = false;
        
        // Chronicle-specific properties
        public int EscalationCounters { get; set; } = 0;
        public int CulminationCost { get; set; } = 0;

        public Card() { } // Needed for deserialization

        public Card(CardData data)
        {
            CardId = data.CardID;
            Name = data.Name;
            CardText = data.CardText;
            Type = data.CardType;
            Faction = data.Faction;
            Cost = data.Cost;
            Attack = data.Attack;
            Defense = data.Defence; // Note: CardData uses "Defence"
            Tier = data.Tier;
            IsUnique = !string.IsNullOrEmpty(data.Unique) && data.Unique.ToLower() == "true";
            
            // Determine deck type based on card type
            DeckType = (data.CardType == "Villager" || data.CardType == "Settlement") ? "Civic" : "Army";
            Position = DeckType == "Civic" ? CardPosition.CivicHand : CardPosition.ArmyHand;
        }
        
        // Helper methods
        public bool CanBeDeployed(int playerTier, int availableMana)
        {
            int requiredTier = GetTierNumber();
            int requiredMana = Cost;
            
            // Iron Price: can deploy one tier higher by paying tier as additional mana
            if (requiredTier == playerTier + 1)
            {
                requiredMana += requiredTier;
            }
            else if (requiredTier > playerTier + 1)
            {
                return false; // Too high tier even with Iron Price
            }
            
            return availableMana >= requiredMana;
        }
        
        public int GetTierNumber()
        {
            return Tier switch
            {
                "I" => 1,
                "II" => 2,
                "III" => 3,
                "IV" => 4,
                _ => 1
            };
        }
        
        public int GetManaCost(int playerTier)
        {
            int requiredTier = GetTierNumber();
            int manaCost = Cost;
            
            // Iron Price: pay tier as additional mana for one tier higher
            if (requiredTier == playerTier + 1)
            {
                manaCost += requiredTier;
            }
            
            return manaCost;
        }
        
        public bool IsUnit()
        {
            return Type != "Tactic" && Type != "Battle Tactic" && Type != "Chronicle" && 
                   Type != "Villager" && Type != "Settlement";
        }
        
        public bool IsCivicCard()
        {
            return Type == "Villager" || Type == "Settlement";
        }
        
        public bool CanBePlayedInBattlePhase()
        {
            return Type == "Battle Tactic";
        }
    }
}
