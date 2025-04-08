using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using Empire.Server.Interfaces;
using Empire.Shared.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Empire.Server.Services
{
    public class DeckLoaderService
    {
        private readonly ILogger<DeckLoaderService> _logger;
        private readonly ICardDatabaseService _cardDb;
        private readonly string _imagePath; // Add this

        public DeckLoaderService(
            IHostEnvironment env,
            ILogger<DeckLoaderService> logger,
            ICardDatabaseService cardDb)
        {
            _logger = logger;
            _cardDb = cardDb;
            _imagePath = Path.Combine(env.ContentRootPath, "wwwroot", "images", "Cards"); // And this
        }

        public List<Card> ParseDeckFromCsv(Stream csvStream)
        {
            var cardIds = new List<int>();

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

                if (!int.TryParse(cardIdString, out int cardId))
                {
                    _logger.LogError("Invalid CardId: {CardIdString}", cardIdString);
                    continue;
                }

                if (!int.TryParse(countString, out int count))
                {
                    _logger.LogError("Invalid Count: {CountString}", countString);
                    continue;
                }

                for (int i = 0; i < count; i++)
                {
                    cardIds.Add(cardId);
                }

                _logger.LogInformation("Parsed card {CardId} x{Count}", cardId, count);
            }

            // Translate card IDs to Card objects
            var cards = _cardDb.GetAllCards()
                .Where(cd => cardIds.Contains(cd.CardID))
                .Select(cd => new Card
                {
                    CardId = cd.CardID,
                    Name = cd.Name,
                    CardText = cd.CardText,
                    Faction = cd.Faction,
                    Type = cd.CardType,
                    ImagePath = cd.ImageFileName ?? "images/Cards/placeholder.jpg"
                })
                .ToList();

            _logger.LogInformation("Parsed {CardCount} cards from CSV", cards.Count);
            return cards;
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