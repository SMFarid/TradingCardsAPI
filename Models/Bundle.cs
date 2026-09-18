using System.ComponentModel.DataAnnotations;

namespace TradingCardsAPI.Models;

/// <summary>A sellable set of cards a user lists; visible to others only when IsPublic.</summary>
public class Bundle
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BundleCard>? Items { get; set; }
}
