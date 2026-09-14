using System.ComponentModel.DataAnnotations;

namespace TradingCardsAPI.Models;

public class Collection
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CollectionCard>? Items { get; set; }
}
