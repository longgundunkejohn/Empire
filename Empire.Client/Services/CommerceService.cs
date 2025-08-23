using System.Net.Http.Json;
using System.Text.Json;

namespace Empire.Client.Services
{
    public class CommerceService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthService _authService;

        public CommerceService(HttpClient httpClient, AuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
        }

        // Get products from WooCommerce
        public async Task<List<Product>> GetProductsAsync(string category = "")
        {
            try
            {
                var url = "/wp-json/wc/v3/products";
                if (!string.IsNullOrEmpty(category))
                {
                    url += $"?category={category}";
                }

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Product>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<Product>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching products: {ex.Message}");
            }

            return new List<Product>();
        }

        // Get booster packs for the game
        public async Task<List<Product>> GetBoosterPacksAsync()
        {
            return await GetProductsAsync("booster-packs");
        }

        // Get individual cards for sale
        public async Task<List<Product>> GetCardsForSaleAsync()
        {
            return await GetProductsAsync("single-cards");
        }

        // Get starter decks
        public async Task<List<Product>> GetStarterDecksAsync()
        {
            return await GetProductsAsync("starter-decks");
        }

        // Create Stripe checkout session (integrated with WordPress)
        public async Task<CheckoutSession?> CreateCheckoutSessionAsync(List<CartItem> items)
        {
            try
            {
                if (!await _authService.IsAuthenticatedAsync())
                {
                    throw new InvalidOperationException("User must be authenticated to checkout");
                }

                var user = await _authService.GetCurrentUserAsync();
                var checkoutData = new
                {
                    items = items,
                    customer_email = user?.Username + "@empire.local", // Temporary email
                    success_url = "https://empirecardgame.com/checkout/success",
                    cancel_url = "https://empirecardgame.com/shop"
                };

                var response = await _httpClient.PostAsJsonAsync("/wp-json/empire/v1/checkout", checkoutData);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<CheckoutSession>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating checkout session: {ex.Message}");
            }

            return null;
        }

        // Add product to cart (integrate with WordPress session)
        public async Task<bool> AddToCartAsync(int productId, int quantity = 1)
        {
            try
            {
                var cartData = new { product_id = productId, quantity = quantity };
                var response = await _httpClient.PostAsJsonAsync("/wp-json/wc/store/cart/add-item", cartData);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding to cart: {ex.Message}");
                return false;
            }
        }

        // Get current cart contents
        public async Task<Cart?> GetCartAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/wp-json/wc/store/cart");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Cart>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching cart: {ex.Message}");
            }

            return null;
        }

        // Sync game account with WordPress user
        public async Task<bool> SyncWithWordPressAccountAsync()
        {
            try
            {
                if (!await _authService.IsAuthenticatedAsync())
                    return false;

                var user = await _authService.GetCurrentUserAsync();
                if (user == null) return false;

                var syncData = new
                {
                    empire_username = user.Username,
                    empire_user_id = user.Id,
                    sync_timestamp = DateTime.UtcNow
                };

                var response = await _httpClient.PostAsJsonAsync("/wp-json/empire/v1/sync-account", syncData);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error syncing with WordPress account: {ex.Message}");
                return false;
            }
        }
    }
}