using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingCardsAPI.Data;
using TradingCardsAPI.DTOs;
using TradingCardsAPI.Models;

namespace TradingCardsAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrderController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserOrders(int userId)
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems!)
                .ThenInclude(i => i.Card)
            .Where(o => o.BuyerId == userId || o.SellerId == userId)
            .ToListAsync();
            
        return Ok(orders);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var order = new Order
        {
            BuyerId = dto.BuyerId,
            SellerId = dto.SellerId,
            Status = "Pending",
            TotalPrice = dto.Items.Sum(i => i.Price * i.Quantity),
            OrderItems = dto.Items.Select(i => new OrderItem
            {
                CardId = i.CardId,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList()
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        return Ok(order);
    }

    [HttpPut("{orderId}/status")]
    public async Task<IActionResult> UpdateStatus(int orderId, [FromBody] UpdateOrderStatusDto dto)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return NotFound();

        order.Status = dto.Status;
        await _context.SaveChangesAsync();
        
        return Ok(order);
    }
}
