using System.ComponentModel.DataAnnotations;

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

    public ICollection<SellerStock>? SellerStocks { get; set; }
}
