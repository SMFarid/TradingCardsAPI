namespace TradingCardsAPI.DTOs;

public class AddStockDto
{
    public int CardId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class CreateOrderDto
{
    public int SellerId { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public int CardId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class UpdateOrderStatusDto
{
    public string Status { get; set; } = string.Empty;
}
