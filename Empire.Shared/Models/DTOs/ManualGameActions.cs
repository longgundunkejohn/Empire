using System;
using System.Collections.Generic;

namespace Empire.Shared.Models.DTOs
{
    // Base action for all manual game actions
    public abstract class ManualGameAction
    {
        public string PlayerId { get; set; } = string.Empty;
        public string GameId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // Priority System Actions
    public class PassPriorityAction : ManualGameAction
    {
        public string ActionDescription => "Pass Priority";
    }

    public class PassInitiativeAction : ManualGameAction
    {
        public string ActionDescription => "Pass Initiative";
    }

    public class AdvancePhaseAction : ManualGameAction
    {
        public GamePhase NewPhase { get; set; }
        public string ActionDescription => $"Advance to {NewPhase} Phase";
    }

    public class AdvanceRoundAction : ManualGameAction
    {
        public int NewRound { get; set; }
        public string ActionDescription => $"Advance to Round {NewRound}";
    }

    // Card Movement Actions
    public class MoveCardAction : ManualGameAction
    {
        public int CardId { get; set; }
        public string FromZone { get; set; } = string.Empty;
        public string ToZone { get; set; } = string.Empty;
        public int? Position { get; set; } // Optional position within zone
        public string ActionDescription => $"Move card from {FromZone} to {ToZone}";
    }

    public class MoveMultipleCardsAction : ManualGameAction
    {
        public List<int> CardIds { get; set; } = new();
        public string FromZone { get; set; } = string.Empty;
        public string ToZone { get; set; } = string.Empty;
        public string ActionDescription => $"Move {CardIds.Count} cards from {FromZone} to {ToZone}";
    }

    // Card State Actions
    public class ToggleCardTappedAction : ManualGameAction
    {
        public int CardId { get; set; }
        public bool IsTapped { get; set; }
        public string ActionDescription => IsTapped ? "Tap card" : "Untap card";
    }

    public class FlipCardAction : ManualGameAction
    {
        public int CardId { get; set; }
        public bool FaceUp { get; set; }
        public string ActionDescription => FaceUp ? "Flip card face up" : "Flip card face down";
    }

    public class AddCounterAction : ManualGameAction
    {
        public int CardId { get; set; }
        public string CounterType { get; set; } = string.Empty; // "+1/+1", "damage", "escalation", etc.
        public int Amount { get; set; } = 1;
        public string ActionDescription => $"Add {Amount} {CounterType} counter(s)";
    }

    public class RemoveCounterAction : ManualGameAction
    {
        public int CardId { get; set; }
        public string CounterType { get; set; } = string.Empty;
        public int Amount { get; set; } = 1;
        public string ActionDescription => $"Remove {Amount} {CounterType} counter(s)";
    }

    // Player State Actions
    public class AdjustMoraleAction : ManualGameAction
    {
        public int Amount { get; set; } // Can be positive or negative
        public string ActionDescription => Amount >= 0 ? $"Gain {Amount} morale" : $"Lose {Math.Abs(Amount)} morale";
    }

    public class AdjustTierAction : ManualGameAction
    {
        public int NewTier { get; set; }
        public string ActionDescription => $"Set tier to {NewTier}";
    }

    // Deck Actions
    public class DrawCardsAction : ManualGameAction
    {
        public string DeckType { get; set; } = string.Empty; // "Army" or "Civic"
        public int Count { get; set; } = 1;
        public string ActionDescription => $"Draw {Count} {DeckType} card(s)";
    }

    public class ShuffleDeckAction : ManualGameAction
    {
        public string DeckType { get; set; } = string.Empty; // "Army" or "Civic"
        public string ActionDescription => $"Shuffle {DeckType} deck";
    }

    // Batch Actions
    public class UntapAllUnitsAction : ManualGameAction
    {
        public string ActionDescription => "Untap all units";
    }

    public class ReplenishmentAction : ManualGameAction
    {
        public bool DrawArmy { get; set; } // true = draw 1 Army, false = draw 2 Civic
        public string ActionDescription => DrawArmy ? "Replenishment: Draw 1 Army card" : "Replenishment: Draw 2 Civic cards";
    }

    // Communication Actions
    public class PingAction : ManualGameAction
    {
        public string TargetType { get; set; } = string.Empty; // "card", "zone", "territory"
        public string TargetId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ActionDescription => $"Ping {TargetType}: {Message}";
    }

    public class ChatMessageAction : ManualGameAction
    {
        public string Message { get; set; } = string.Empty;
        public string ActionDescription => "Chat message";
    }

    // Zone definitions for validation
    public static class GameZones
    {
        // Player zones
        public const string ArmyHand = "army-hand";
        public const string CivicHand = "civic-hand";
        public const string Heartland = "heartland";
        public const string Villagers = "villagers";
        public const string ArmyDeck = "army-deck";
        public const string CivicDeck = "civic-deck";
        public const string Graveyard = "graveyard";
        public const string SealedZone = "sealed";

        // Territory zones
        public const string Territory1Advancing = "territory-1-advancing";
        public const string Territory1Occupying = "territory-1-occupying";
        public const string Territory1Settlement = "territory-1-settlement";
        public const string Territory2Advancing = "territory-2-advancing";
        public const string Territory2Occupying = "territory-2-occupying";
        public const string Territory2Settlement = "territory-2-settlement";
        public const string Territory3Advancing = "territory-3-advancing";
        public const string Territory3Occupying = "territory-3-occupying";
        public const string Territory3Settlement = "territory-3-settlement";

        public static readonly List<string> AllZones = new()
        {
            ArmyHand, CivicHand, Heartland, Villagers, ArmyDeck, CivicDeck, Graveyard, SealedZone,
            Territory1Advancing, Territory1Occupying, Territory1Settlement,
            Territory2Advancing, Territory2Occupying, Territory2Settlement,
            Territory3Advancing, Territory3Occupying, Territory3Settlement
        };

        public static readonly List<string> PlayerZones = new()
        {
            ArmyHand, CivicHand, Heartland, Villagers, ArmyDeck, CivicDeck, Graveyard, SealedZone
        };

        public static readonly List<string> TerritoryZones = new()
        {
            Territory1Advancing, Territory1Occupying, Territory1Settlement,
            Territory2Advancing, Territory2Occupying, Territory2Settlement,
            Territory3Advancing, Territory3Occupying, Territory3Settlement
        };
    }
}
