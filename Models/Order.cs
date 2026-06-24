using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingCardsAPI.Models;

public class Order
{
    public int Id { get; set; }

    public int BuyerId { get; set; }
    public User? Buyer { get; set; }

    public int SellerId { get; set; }
    public User? Seller { get; set; }

    public string Status { get; set; } = "Pending"; // Pending, Completed, Cancelled

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem>? OrderItems { get; set; }
}
