namespace TradingCardsAPI.Models;

public class CollectionCard
{
    public int Id { get; set; }
    public int CollectionId { get; set; }
    public Collection? Collection { get; set; }
    public int CardId { get; set; }
    public Card? Card { get; set; }
    public int Quantity { get; set; } = 1;
}
