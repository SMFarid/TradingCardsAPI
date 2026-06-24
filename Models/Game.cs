using System.ComponentModel.DataAnnotations;

namespace TradingCardsAPI.Models;

public class Game
{
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    public ICollection<Card>? Cards { get; set; }
}
