using CryptoShop.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CryptoShop.Data;

public class CryptoShopDbContext(DbContextOptions<CryptoShopDbContext> options) : DbContext(options)
{
    public DbSet<CryptoProduct> Products => Set<CryptoProduct>();
    public DbSet<CryptoPurchaseOrder> PurchaseOrders => Set<CryptoPurchaseOrder>();
    public DbSet<CryptoSettlementRecord> SettlementRecords => Set<CryptoSettlementRecord>();
    public DbSet<WalletSessionEntity> WalletSessions => Set<WalletSessionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CryptoPurchaseOrder>().HasKey(o => o.Id);
        modelBuilder.Entity<CryptoSettlementRecord>().HasKey(r => r.SettlementId);
        modelBuilder.Entity<WalletSessionEntity>().HasKey(s => s.SessionId);

        // Seed catalog data
        // PriceTon expressed in nanotons: 1 TON = 1_000_000_000
        modelBuilder.Entity<CryptoProduct>().HasData(
            new CryptoProduct
            {
                Id = 1,
                Name = "Lucky Dice Skin",
                Description = "Exclusive animated dice skin for in-game rolls.",
                PriceStars = 10,
                PriceTon = 50_000_000, // 0.05 TON
                PriceUsdt = 100_000, // 0.10 USDT
                IsAvailable = true
            },
            new CryptoProduct
            {
                Id = 2,
                Name = "VIP Lounge Pass (7 days)",
                Description = "Access to the VIP chat lounge for 7 days.",
                PriceStars = 25,
                PriceTon = 100_000_000, // 0.10 TON
                PriceUsdt = 250_000, // 0.25 USDT
                IsAvailable = true
            },
            new CryptoProduct
            {
                Id = 3,
                Name = "Neon Avatar Frame",
                Description = "Animated neon border for your profile photo.",
                PriceStars = 15,
                PriceTon = 70_000_000, // 0.07 TON
                PriceUsdt = 150_000, // 0.15 USDT
                IsAvailable = true
            },
            new CryptoProduct
            {
                Id = 4,
                Name = "Double XP Boost (24h)",
                Description = "Double your experience points for 24 hours.",
                PriceStars = 5,
                PriceTon = 25_000_000, // 0.025 TON
                PriceUsdt = 50_000, // 0.05 USDT
                IsAvailable = true
            });
    }
}
