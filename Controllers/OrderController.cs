using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingCardsAPI.Data;
using TradingCardsAPI.DTOs;
using TradingCardsAPI.Models;

namespace TradingCardsAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrderController(AppDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = CurrentUserId;
        var orders = await _context.Orders
            .Where(o => o.BuyerId == userId || o.SellerId == userId)
            .Select(o => new
            {
                o.Id,
                o.BuyerId,
                o.SellerId,
                o.Status,
                o.TotalPrice,
                o.CreatedAt,
                Items = o.OrderItems!.Select(i => new
                {
                    i.Id,
                    i.Quantity,
                    i.Price,
                    Card = new { i.Card!.Id, i.Card.Name, i.Card.Code, i.Card.ImageUrl }
                })
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var buyerId = CurrentUserId;

        if (dto.Items.Count == 0) return BadRequest("Order has no items.");
        if (!await _context.Users.AnyAsync(u => u.Id == dto.SellerId))
            return BadRequest("Seller not found.");

        var order = new Order
        {
            BuyerId = buyerId,
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

        return Ok(new { order.Id, order.BuyerId, order.SellerId, order.Status, order.TotalPrice, order.CreatedAt });
    }

    [HttpPut("{orderId}/status")]
    public async Task<IActionResult> UpdateStatus(int orderId, [FromBody] UpdateOrderStatusDto dto)
    {
        var userId = CurrentUserId;
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId
                                      && (o.BuyerId == userId || o.SellerId == userId));
        if (order == null) return NotFound();

        var allowed = new[] { "Pending", "Shipped", "Completed", "Cancelled" };
        if (!allowed.Contains(dto.Status))
            return BadRequest($"Status must be one of: {string.Join(", ", allowed)}.");

        order.Status = dto.Status;
        await _context.SaveChangesAsync();

        return Ok(new { order.Id, order.Status });
    }
}
