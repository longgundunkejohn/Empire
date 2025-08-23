namespace Empire.Client.Services
{
    // Shared commerce models to avoid duplicates across service files
    
    public class WooCommerceProduct
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string ShortDescription { get; set; } = "";
        public decimal Price { get; set; }
        public string Sku { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public List<string> Categories { get; set; } = new();
        public Dictionary<string, object> Attributes { get; set; } = new();
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Price { get; set; } = "";
        public string RegularPrice { get; set; } = "";
        public string SalePrice { get; set; } = "";
        public bool OnSale { get; set; }
        public string Sku { get; set; } = "";
        public List<ProductImage> Images { get; set; } = new();
        public List<ProductCategory> Categories { get; set; } = new();
        public Dictionary<string, object> Attributes { get; set; } = new();
    }

    public class ProductImage
    {
        public int Id { get; set; }
        public string Src { get; set; } = "";
        public string Alt { get; set; } = "";
    }

    public class ProductCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
    }

    public class CartItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string Name { get; set; } = "";
    }

    public class Cart
    {
        public List<CartItem> Items { get; set; } = new();
        public decimal Total { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Shipping { get; set; }
        public int ItemsCount { get; set; }
    }

    public class CheckoutSession
    {
        public string Id { get; set; } = "";
        public string Url { get; set; } = "";
        public string PaymentStatus { get; set; } = "";
    }

    // WordPress specific models
    public class BlogPost
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string Excerpt { get; set; } = "";
        public string Slug { get; set; } = "";
        public DateTime Date { get; set; }
        public string Author { get; set; } = "";
        public List<string> Categories { get; set; } = new();
        public List<string> Tags { get; set; } = new();
    }

    public class GameUpdate
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime Date { get; set; }
    }

    public class CardPreview
    {
        public string Name { get; set; } = "";
        public int Cost { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public string Tier { get; set; } = "";
        public string Description { get; set; } = "";
    }
}