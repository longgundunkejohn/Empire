using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
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

            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                string cardIdString = csv.GetField("Card ID");
                string countString = csv.GetField("Count");

                countString = countString.Trim();

                if (!int.TryParse(cardIdString, out int cardId))
                {
                    _logger.LogError("Invalid CardId: {CardId}", cardIdString);
                    continue;
                }

                if (!int.TryParse(countString, out int count))
                {
                    _logger.LogError("Invalid Count: {Count}", countString);
                    continue;
                }

                // Card name is no longer read from CSV
                // var cardName = csv.GetField("Card Name");

                var target = IsCivicCard(cardId) ? civic : military;
                for (int i = 0; i < count; i++)
                {
                    target.Add(cardId);
                }

                _logger.LogInformation("Parsed card {CardId} x{Count}", cardId, count);
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