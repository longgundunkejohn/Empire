using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Empire.Server.Interfaces;
using Empire.Shared.Models;
using Empire.Shared.Models.DTOs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Empire.Server.Services
{
    public class DeckLoaderService
    {
        private readonly ILogger<DeckLoaderService> _logger;
        private readonly string _imagePath;

        public DeckLoaderService(
            IHostEnvironment env,
            ILogger<DeckLoaderService> logger)
        {
            _logger = logger;
            _imagePath = Path.Combine(env.ContentRootPath, "wwwroot", "images", "Cards");
        }

        private bool IsCivicCard(int cardId)
        {
            int lastTwoDigits = cardId % 100;
            return lastTwoDigits is >= 80 and <= 99;
        }

        public (List<int> CivicDeck, List<int> MilitaryDeck) ParseDeckFromCsv(Stream csvStream)
        {
            var civic = new List<int>();
            var military = new List<int>();

            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null,
                TrimOptions = TrimOptions.Trim,
            });

            //  Instead of directly mapping to RawDeckEntry, read rows dynamically
            while (csv.Read())
            {
                //  Error handling: Ensure we have at least 3 columns
                if (csv.Context.Record.Length < 3)
                {
                    _logger.LogError("Invalid CSV row: Not enough columns.");
                    continue; // Skip this row
                }

                //  Parse CardId and Count (these are assumed to be safe)
                if (!int.TryParse(csv[0], out int cardId))
                {
                    _logger.LogError("Invalid CardId: {CardId}", csv[0]);
                    continue;
                }

                if (!int.TryParse(csv[csv.Context.Record.Length - 1], out int count))
                {
                    _logger.LogError("Invalid Count: {Count}", csv[csv.Context.Record.Length - 1]);
                    continue;
                }

                //  Reconstruct CardName (handles the variable columns in between)
                var cardName = string.Join(",", csv.Context.Record.Skip(1).Take(csv.Context.Record.Length - 2)).Trim();


                var target = IsCivicCard(cardId) ? civic : military;
                for (int i = 0; i < count; i++)
                {
                    target.Add(cardId);
                }

                _logger.LogInformation("Parsed card {CardId} ({CardName}) x{Count}", cardId, cardName, count);
            }

            _logger.LogInformation("Final CivicDeck: {CivicDeck}", string.Join(", ", civic));
            _logger.LogInformation("Final MilitaryDeck: {MilitaryDeck}", string.Join(", ", military));

            return (civic, military);
        }

        public string GetImagePath(int cardId)
        {
            var filePath = Path.Combine(_imagePath, $"{cardId}.jpg");
            return File.Exists(filePath)
                ? $"images/Cards/{cardId}.jpg"
                : "images/Cards/placeholder.jpg";
        }
    }
}