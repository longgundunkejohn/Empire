using CsvHelper.Configuration;
using Empire.Shared.Models.DTOs;

public class RawDeckEntryMap : ClassMap<RawDeckEntry>
{
    public RawDeckEntryMap()
    {
        Map(m => m.CardId).Name("Card ID");
        Map(m => m.Count).Name("Count");
    }
}
