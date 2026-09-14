using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingCardsAPI.Models;

public class Card
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public Game? Game { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsFoil { get; set; } = false;

    /// <summary>TCGplayer market price in USD (via Scryfall); null when no source covers the game.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MarketPrice { get; set; }

    public ICollection<SellerStock>? SellerStocks { get; set; }
}
