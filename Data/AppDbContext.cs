using Microsoft.EntityFrameworkCore;
using TradingCardsAPI.Models;

namespace TradingCardsAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<Card> Cards { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Collection> Collections { get; set; }
    public DbSet<CollectionCard> CollectionCards { get; set; }
    public DbSet<Bundle> Bundles { get; set; }
    public DbSet<BundleCard> BundleCards { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CollectionCard>()
            .HasIndex(cc => new { cc.CollectionId, cc.CardId })
            .IsUnique();

        modelBuilder.Entity<BundleCard>()
            .HasIndex(bc => new { bc.BundleId, bc.CardId })
            .IsUnique();

        // Order history must survive a listing being deleted.
        modelBuilder.Entity<OrderItem>()
            .HasOne(i => i.BundleCard)
            .WithMany()
            .HasForeignKey(i => i.BundleCardId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Buyer)
            .WithMany(u => u.BuyerOrders)
            .HasForeignKey(o => o.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Seller)
            .WithMany(u => u.SellerOrders)
            .HasForeignKey(o => o.SellerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
