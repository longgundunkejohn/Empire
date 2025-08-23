using Empire.Shared.Models;
using System.Text.Json;

namespace Empire.Client.Services
{
    public class CardDataService
    {
        private readonly HttpClient _httpClient;
        private List<CardData>? _allCards;
        private Dictionary<int, CardData>? _cardLookup;

        public CardDataService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<CardData>> GetAllCardsAsync()
        {
            if (_allCards == null)
            {
                await LoadCardsAsync();
            }
            return _allCards ?? new List<CardData>();
        }

        public async Task<CardData?> GetCardByIdAsync(int cardId)
        {
            if (_cardLookup == null)
            {
                await LoadCardsAsync();
            }
            return _cardLookup?.GetValueOrDefault(cardId);
        }

        public async Task<List<CardData>> GetCardsByTypeAsync(string cardType)
        {
            var allCards = await GetAllCardsAsync();
            return allCards.Where(c => c.CardType.Contains(cardType, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task<List<CardData>> GetCardsByTierAsync(string tier)
        {
            var allCards = await GetAllCardsAsync();
            return allCards.Where(c => c.Tier == tier).ToList();
        }

        public async Task<List<CardData>> GetCardsByFactionAsync(string faction)
        {
            var allCards = await GetAllCardsAsync();
            return allCards.Where(c => c.Faction.Equals(faction, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task<List<CardData>> SearchCardsAsync(string searchTerm)
        {
            var allCards = await GetAllCardsAsync();
            return allCards.Where(c => 
                c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                c.CardText.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                c.CardType.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        // ============ ENHANCED METHODS FOR CMS INTEGRATION ============

        /// <summary>
        /// Get cards suitable for e-commerce display with images
        /// </summary>
        public async Task<List<CardData>> GetCardsForShopAsync()
        {
            var allCards = await GetAllCardsAsync();
            return allCards.Where(c => !string.IsNullOrEmpty(c.ImageFileName) && 
                                     !string.IsNullOrEmpty(c.Name)).ToList();
        }

        /// <summary>
        /// Get featured cards for WordPress homepage
        /// </summary>
        public async Task<List<CardData>> GetFeaturedCardsAsync(int count = 6)
        {
            var allCards = await GetCardsForShopAsync();
            
            // Get a mix of different types and tiers
            var featured = new List<CardData>();
            
            // Add some high-tier cards
            featured.AddRange(allCards.Where(c => c.Tier == "3" || c.Tier == "2").Take(count / 2));
            
            // Add some popular unit types
            featured.AddRange(allCards.Where(c => c.CardType.Contains("Unit") && !featured.Contains(c)).Take(count / 2));
            
            // Fill remaining slots with random cards
            var remaining = allCards.Except(featured).Take(count - featured.Count);
            featured.AddRange(remaining);
            
            return featured.Take(count).ToList();
        }

        /// <summary>
        /// Get card data formatted for WordPress product creation
        /// </summary>
        public async Task<List<WooCommerceProduct>> GetCardsAsProductsAsync()
        {
            var cards = await GetCardsForShopAsync();
            
            return cards.Select(card => new WooCommerceProduct
            {
                Name = card.Name,
                Description = FormatCardDescription(card),
                ShortDescription = $"{card.CardType} - {card.Tier} Tier",
                Price = CalculateCardPrice(card),
                Sku = $"CARD-{card.CardID}",
                ImageUrl = GetCardImageUrl(card),
                Categories = GetCardCategories(card),
                Attributes = GetCardAttributes(card)
            }).ToList();
        }

        public async Task<List<CardData>> GetUnitsAsync()
        {
            return await GetCardsByTypeAsync("Unit");
        }

        public async Task<List<CardData>> GetTacticsAsync()
        {
            return await GetCardsByTypeAsync("Tactic");
        }

        public async Task<List<CardData>> GetChroniclesAsync()
        {
            return await GetCardsByTypeAsync("Chronicle");
        }

        public async Task<List<CardData>> GetSettlementsAsync()
        {
            return await GetCardsByTypeAsync("Settlement");
        }

        public async Task<List<CardData>> GetVillagersAsync()
        {
            return await GetCardsByTypeAsync("Villager");
        }

        // ============ IMAGE HANDLING WITH CMS SUPPORT ============

        public string GetCardImageUrl(int cardId, bool usePlaceholder = true)
        {
            // Check if running in WordPress context
            var baseUrl = GetImageBaseUrl();
            var imagePath = $"{baseUrl}/images/Cards/{cardId}.jpg";
            
            if (!usePlaceholder)
            {
                return imagePath;
            }
            
            // Return placeholder path for missing images
            return $"{baseUrl}/images/Cards/placeholder.jpg";
        }

        public string GetCardImageUrl(CardData card, bool usePlaceholder = true)
        {
            return GetCardImageUrl(card.CardID, usePlaceholder);
        }

        /// <summary>
        /// Get high-resolution image URL for WordPress product gallery
        /// </summary>
        public string GetCardImageUrlHighRes(CardData card)
        {
            var baseUrl = GetImageBaseUrl();
            return $"{baseUrl}/images/Cards/hires/{card.CardID}.jpg";
        }

        /// <summary>
        /// Get thumbnail image URL for WordPress product listings
        /// </summary>
        public string GetCardImageThumbnail(CardData card)
        {
            var baseUrl = GetImageBaseUrl();
            return $"{baseUrl}/images/Cards/thumbs/{card.CardID}_thumb.jpg";
        }

        private string GetImageBaseUrl()
        {
            // Detect if running in WordPress iframe context
            try
            {
                // Check current URL to determine context
                var currentUrl = _httpClient.BaseAddress?.ToString() ?? "";
                
                if (currentUrl.Contains("/play/") || currentUrl.Contains("wp-"))
                {
                    // Running in WordPress context - use game API base
                    return "/game-api";
                }
                
                return "";
            }
            catch
            {
                return "";
            }
        }

        // ============ PRIVATE HELPER METHODS ============

        private async Task LoadCardsAsync()
        {
            try
            {
                // Try to load from local JSON file first (for development)
                var jsonString = await _httpClient.GetStringAsync("/empire_cards.json");
                var cards = JsonSerializer.Deserialize<List<CardDataDto>>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (cards != null)
                {
                    _allCards = cards.Select(ConvertFromDto).ToList();
                    _cardLookup = _allCards.ToDictionary(c => c.CardID, c => c);
                }
            }
            catch
            {
                // Fallback to API if local file doesn't exist
                try
                {
                    var response = await _httpClient.GetAsync("/api/deckbuilder/cards");
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        var cards = JsonSerializer.Deserialize<List<CardData>>(jsonString, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        
                        if (cards != null)
                        {
                            _allCards = cards;
                            _cardLookup = _allCards.ToDictionary(c => c.CardID, c => c);
                        }
                    }
                }
                catch
                {
                    // If all else fails, return empty list
                    _allCards = new List<CardData>();
                    _cardLookup = new Dictionary<int, CardData>();
                }
            }
        }

        private CardData ConvertFromDto(CardDataDto dto)
        {
            return new CardData
            {
                CardID = dto.CardID,
                Name = dto.Name ?? "",
                CardText = dto.CardText ?? "",
                CardType = dto.CardType ?? "",
                Tier = dto.Tier ?? "",
                Cost = dto.Cost,
                Attack = dto.Attack,
                Defence = dto.Defence,
                Unique = dto.Unique ?? "No",
                Faction = dto.Faction ?? "No",
                ImageFileName = $"{dto.CardID}.jpg"
            };
        }

        // ============ WORDPRESS INTEGRATION HELPERS ============

        private string FormatCardDescription(CardData card)
        {
            var description = $"<strong>{card.Name}</strong><br/>";
            description += $"<em>{card.CardType} - Tier {card.Tier}</em><br/><br/>";
            
            if (card.Attack > 0 || card.Defence > 0)
            {
                description += $"<strong>Combat:</strong> {card.Attack}/{card.Defence}<br/>";
            }
            
            if (card.Cost > 0)
            {
                description += $"<strong>Cost:</strong> {card.Cost}<br/>";
            }
            
            if (!string.IsNullOrEmpty(card.CardText))
            {
                description += $"<br/><strong>Effect:</strong> {card.CardText}";
            }
            
            return description;
        }

        private decimal CalculateCardPrice(CardData card)
        {
            // Price based on tier and type
            decimal basePrice = card.Tier switch
            {
                "1" => 2.99m,
                "2" => 4.99m,
                "3" => 7.99m,
                _ => 1.99m
            };
            
            // Adjust for card type
            if (card.CardType.Contains("Chronicle"))
                basePrice *= 1.5m;
            else if (card.Unique == "Yes")
                basePrice *= 1.3m;
                
            return Math.Round(basePrice, 2);
        }

        private List<string> GetCardCategories(CardData card)
        {
            var categories = new List<string> { "Single Cards" };
            
            categories.Add($"Tier {card.Tier}");
            categories.Add(card.CardType);
            
            if (card.Faction != "No" && !string.IsNullOrEmpty(card.Faction))
            {
                categories.Add(card.Faction);
            }
            
            if (card.Unique == "Yes")
            {
                categories.Add("Unique Cards");
            }
            
            return categories;
        }

        private Dictionary<string, object> GetCardAttributes(CardData card)
        {
            return new Dictionary<string, object>
            {
                { "card_id", card.CardID },
                { "tier", card.Tier },
                { "card_type", card.CardType },
                { "cost", card.Cost },
                { "attack", card.Attack },
                { "defence", card.Defence },
                { "unique", card.Unique },
                { "faction", card.Faction }
            };
        }
    }

    // DTO class to match the MongoDB structure
    public class CardDataDto
    {
        public string? _id { get; set; }
        public int CardID { get; set; }
        public string? Name { get; set; }
        public string? CardText { get; set; }
        public string? CardType { get; set; }
        public string? Tier { get; set; }
        public int Cost { get; set; }
        public int Attack { get; set; }
        public int Defence { get; set; }
        public string? Unique { get; set; }
        public string? Faction { get; set; }
        public string? ImageFileName { get; set; }
    }
}
