using System.ComponentModel.DataAnnotations;

namespace TradingCardsAPI.DTOs;

public class CreateBundleDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
}

public class UpdateBundleDto
{
    public string? Name { get; set; }
    public bool? IsPublic { get; set; }
}

public class ImportCollectionsDto
{
    [Required]
    public List<int> CollectionIds { get; set; } = new();
}

public class UpdateBundleCardDto
{
    public decimal? Price { get; set; }
    public int? Quantity { get; set; }
}

public class CheckoutDto
{
    [Required]
    public List<CheckoutItemDto> Items { get; set; } = new();
}

public class CheckoutItemDto
{
    public int BundleCardId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdateOrderStatusDto
{
    public string Status { get; set; } = string.Empty;
}
