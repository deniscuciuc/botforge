using Microsoft.EntityFrameworkCore;

namespace TelegramShop.Data;

public class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasData(
                new Product
                {
                    Id = 1,
                    Name = "Digital Sticker Pack",
                    Description = "A pack of 50 custom stickers",
                    PriceStars = 10,
                    Category = "Digital"
                },
                new Product
                {
                    Id = 2,
                    Name = "Premium Badge",
                    Description = "Exclusive premium badge for your profile",
                    PriceStars = 25,
                    Category = "Digital"
                },
                new Product
                {
                    Id = 3,
                    Name = "Custom Emoji Set",
                    Description = "Set of 20 custom animated emojis",
                    PriceStars = 15,
                    Category = "Digital"
                },
                new Product
                {
                    Id = 4,
                    Name = "Bot Theme: Dark",
                    Description = "Dark theme for your bot interactions",
                    PriceStars = 5,
                    Category = "Themes"
                },
                new Product
                {
                    Id = 5,
                    Name = "Bot Theme: Neon",
                    Description = "Neon-colored theme with glow effects",
                    PriceStars = 8,
                    Category = "Themes"
                },
                new Product
                {
                    Id = 6,
                    Name = "VIP Membership (1 month)",
                    Description = "Access to exclusive VIP features",
                    PriceStars = 50,
                    Category = "Membership"
                }
            );
        });

        modelBuilder.Entity<CartItem>(e => { e.HasKey(c => new { c.UserId, c.ProductId }); });

        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.UserId);
        });

        modelBuilder.Entity<OrderItem>(e => { e.HasKey(oi => oi.Id); });
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int PriceStars { get; set; }
    public string Category { get; set; } = "";
    public bool IsAvailable { get; set; } = true;
}

public class CartItem
{
    public long UserId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public Product? Product { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public int TotalStars { get; set; }
    public string Status { get; set; } = "completed";
    public string? TelegramChargeId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int PriceStars { get; set; }
    public int Quantity { get; set; }
}
