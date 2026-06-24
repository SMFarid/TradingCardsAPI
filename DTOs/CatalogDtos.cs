namespace TradingCardsAPI.DTOs;

public class GameDto
{
    public string Name { get; set; } = string.Empty;
}

public class CardDto
{
    public int GameId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}
