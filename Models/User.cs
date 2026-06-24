using System.ComponentModel.DataAnnotations;

namespace TradingCardsAPI.Models;

public class User
{
    public int Id { get; set; }
    [Required]
    public string Role { get; set; } = "Buyer"; // Buyer, Seller, Both
    [Required]
    public string FullName { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    public ICollection<SellerStock>? SellerStocks { get; set; }
    public ICollection<Order>? BuyerOrders { get; set; }
    public ICollection<Order>? SellerOrders { get; set; }
}
