using GiftCommerceBot.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GiftCommerceBot.Data;

public class GiftShopDbContext(DbContextOptions<GiftShopDbContext> options) : DbContext(options)
{
    public DbSet<GiftProduct> Products => Set<GiftProduct>();
    public DbSet<GiftPurchaseOrder> PurchaseOrders => Set<GiftPurchaseOrder>();
    public DbSet<GiftSettlementRecord> SettlementRecords => Set<GiftSettlementRecord>();
    public DbSet<GiftIntakeEntry> GiftIntakes => Set<GiftIntakeEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GiftProduct>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasData(
                new GiftProduct
                {
                    Id = 1,
                    Name = "Lucky Dice Skin",
                    Description = "Custom animated dice skin for all mini-games",
                    PriceStars = 10,
                    GiftIds = "AgAD..."
                },
                new GiftProduct
                {
                    Id = 2,
                    Name = "VIP Lounge (7 days)",
                    Description = "Access to the VIP lounge chat for a week",
                    PriceStars = 25,
                    GiftIds = ""
                },
                new GiftProduct
                {
                    Id = 3,
                    Name = "Neon Avatar Frame",
                    Description = "Glowing neon frame displayed next to your name",
                    PriceStars = 15,
                    GiftIds = ""
                },
                new GiftProduct
                {
                    Id = 4,
                    Name = "Double XP Boost (24h)",
                    Description = "Double experience points for 24 hours",
                    PriceStars = 5,
                    GiftIds = ""
                }
            );
        });

        modelBuilder.Entity<GiftPurchaseOrder>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.UserId);
        });

        modelBuilder.Entity<GiftSettlementRecord>(e => { e.HasKey(s => s.SettlementId); });

        modelBuilder.Entity<GiftIntakeEntry>(e =>
        {
            e.HasKey(g => g.Id);
            e.HasIndex(g => g.OwnedGiftId).IsUnique();
        });
    }
}
