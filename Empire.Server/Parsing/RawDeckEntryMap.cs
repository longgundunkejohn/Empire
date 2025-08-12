using CsvHelper.Configuration;
using Empire.Shared.Models.DTOs;

namespace Empire.Server.Parsing
{
    public class RawDeckEntryMap : ClassMap<RawDeckEntry>
    {
        public RawDeckEntryMap()
        {
            Map(m => m.CardId).Name("CardId", "Card ID", "ID");
            Map(m => m.Count).Name("Count", "Quantity", "Qty");
            Map(m => m.DeckType).Name("DeckType", "Deck Type", "Type");
            Map(m => m.CardName).Name("CardName", "Card Name", "Name");
            Map(m => m.Player).Name("Player", "PlayerName", "Player Name").Optional();
        }
    }
}
