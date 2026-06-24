using Microsoft.EntityFrameworkCore;
using TradingCardsAPI.Models;

namespace TradingCardsAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<Card> Cards { get; set; }
    public DbSet<SellerStock> SellerStocks { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
