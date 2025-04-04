using CsvHelper.Configuration;
using Empire.Shared.Models.DTOs;

namespace Empire.Server.Parsing
{
    public class RawDeckEntryMap : ClassMap<RawDeckEntry>
    {
        public RawDeckEntryMap()
        {
            Map(m => m.CardId).Name("Card ID");
            Map(m => m.CardName).Name("Card Name");
            Map(m => m.Count).Name("Count");
        }
    }
}
