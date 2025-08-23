using System.Net.Http.Json;
using System.Text.Json;

namespace Empire.Client.Services
{
    public class WordPressService
    {
        private readonly HttpClient _httpClient;
        private readonly string _wpApiUrl;

        public WordPressService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Use base URL from httpClient configuration
            _wpApiUrl = "/wp-json/wp/v2/";
        }

        // Get WordPress pages/posts for integration
        public async Task<List<WordPressPost>> GetPostsAsync(string category = "")
        {
            try
            {
                var url = $"{_wpApiUrl}posts";
                if (!string.IsNullOrEmpty(category))
                {
                    url += $"?categories={category}";
                }

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<WordPressPost>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<WordPressPost>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching WordPress posts: {ex.Message}");
            }

            return new List<WordPressPost>();
        }

        // Get blog posts for homepage integration
        public async Task<List<BlogPost>> GetBlogPostsAsync(int page = 1, int perPage = 10)
        {
            try
            {
                var posts = await GetPostsAsync();
                return posts.Take(perPage).Select(p => new BlogPost
                {
                    Id = p.Id,
                    Title = p.Title,
                    Content = p.Content,
                    Excerpt = p.Excerpt,
                    Date = p.Date,
                    Author = "Empire Team", // Default author
                    Categories = p.Categories
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting blog posts: {ex.Message}");
                return new List<BlogPost>();
            }
        }

        // Get game updates for homepage
        public async Task<List<GameUpdate>> GetGameUpdatesAsync(int page = 1, int perPage = 3)
        {
            try
            {
                var posts = await GetPostsAsync("game-updates");
                return posts.Take(perPage).Select(p => new GameUpdate
                {
                    Title = p.Title,
                    Content = p.Excerpt,
                    Date = p.Date
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching game updates: {ex.Message}");
                return new List<GameUpdate>();
            }
        }

        // Get card previews for homepage (mock implementation)
        public async Task<List<CardPreview>> GetCardPreviewsAsync(int page = 1, int perPage = 6)
        {
            try
            {
                // This would typically fetch from a custom post type or API
                // For now, return mock data
                return new List<CardPreview>
                {
                    new() { Name = "Ancient Dragon", Cost = 8, Attack = 12, Defense = 10, Tier = "3", Description = "Legendary creature with devastating fire breath" },
                    new() { Name = "Castle Fortress", Cost = 5, Attack = 0, Defense = 15, Tier = "2", Description = "Massive defensive structure" },
                    new() { Name = "Elite Guard", Cost = 3, Attack = 5, Defense = 4, Tier = "2", Description = "Professional warrior unit" },
                    new() { Name = "Mystic Scholar", Cost = 2, Attack = 1, Defense = 3, Tier = "1", Description = "Knowledge seeker and spell caster" },
                    new() { Name = "Trade Caravan", Cost = 4, Attack = 2, Defense = 6, Tier = "1", Description = "Economic unit providing resources" },
                    new() { Name = "War Machine", Cost = 6, Attack = 8, Defense = 5, Tier = "3", Description = "Mechanical siege weapon" }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching card previews: {ex.Message}");
                return new List<CardPreview>();
            }
        }

        // Get game rules from WordPress
        public async Task<WordPressPost?> GetGameRulesAsync()
        {
            try
            {
                var posts = await GetPostsAsync("rules");
                return posts.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching game rules: {ex.Message}");
                return null;
            }
        }

        // Send user data to WordPress (for cross-platform accounts)
        public async Task<bool> SyncUserWithWordPressAsync(string username, string email)
        {
            try
            {
                var userData = new
                {
                    username = username,
                    email = email,
                    meta = new { empire_tcg_player = true }
                };

                var response = await _httpClient.PostAsJsonAsync($"{_wpApiUrl}users", userData);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error syncing user with WordPress: {ex.Message}");
                return false;
            }
        }
    }

    // WordPress API Models
    public class WordPressPost
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string Excerpt { get; set; } = "";
        public DateTime Date { get; set; }
        public string Link { get; set; } = "";
        public List<string> Categories { get; set; } = new();
    }
}