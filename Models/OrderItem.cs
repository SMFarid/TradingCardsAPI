using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingCardsAPI.Models;

public class OrderItem
{
    public int Id { get; set; }
    
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int CardId { get; set; }
    public Card? Card { get; set; }

    public int Quantity { get; set; } = 1;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
}
