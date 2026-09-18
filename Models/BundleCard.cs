using System.ComponentModel.DataAnnotations.Schema;

namespace TradingCardsAPI.Models;

public class BundleCard
{
    public int Id { get; set; }
    public int BundleId { get; set; }
    public Bundle? Bundle { get; set; }
    public int CardId { get; set; }
    public Card? Card { get; set; }

    /// <summary>Seller's asking price; defaults to the card's market price on import.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    public int Quantity { get; set; } = 1;
}
