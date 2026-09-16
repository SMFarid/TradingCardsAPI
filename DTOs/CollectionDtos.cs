using System.ComponentModel.DataAnnotations;

namespace TradingCardsAPI.DTOs;

public class CreateCollectionDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
}

public class AddCollectionCardDto
{
    public int CardId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class IdentifyCardDto
{
    [Required]
    public List<string> Lines { get; set; } = new();
}

public class ResolveCardDto
{
    [Required]
    public string Game { get; set; } = string.Empty;
    [Required]
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsFoil { get; set; } = false;
    public decimal? MarketPrice { get; set; }
}

public class RenameCollectionDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
}

public class UpdateCollectionCardDto
{
    public int Quantity { get; set; }
}
