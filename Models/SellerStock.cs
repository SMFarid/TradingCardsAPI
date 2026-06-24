using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingCardsAPI.Models;

public class SellerStock
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User? User { get; set; }

    public int CardId { get; set; }
    public Card? Card { get; set; }

    public int Quantity { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UserPrice { get; set; } = 0;
}
