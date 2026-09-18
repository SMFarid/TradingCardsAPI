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
    // Orders are settled in person: Pending (buyer requested) -> Accepted
    // (seller confirmed) -> Completed (exchanged), or Cancelled.
    private const string Pending = "Pending";
    private const string Accepted = "Accepted";
    private const string Completed = "Completed";
    private const string Cancelled = "Cancelled";

    private readonly AppDbContext _context;

    public OrderController(AppDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Creates orders from public bundle listings. Prices come from the listings,
    /// never the client. A cart spanning several sellers is split into one order
    /// per seller. Quantities are validated here but only decremented when the
    /// seller marks the order Completed.
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
    {
        var buyerId = CurrentUserId;
        if (dto.Items.Count == 0) return BadRequest("Cart is empty.");
        if (dto.Items.Any(i => i.Quantity < 1))
            return BadRequest("Quantities must be at least 1.");

        var ids = dto.Items.Select(i => i.BundleCardId).Distinct().ToList();
        if (ids.Count != dto.Items.Count)
            return BadRequest("Duplicate cart entries for the same listing.");

        var listings = await _context.BundleCards
            .Include(bc => bc.Bundle)
            .Include(bc => bc.Card)
            .Where(bc => ids.Contains(bc.Id))
            .ToDictionaryAsync(bc => bc.Id);

        foreach (var item in dto.Items)
        {
            if (!listings.TryGetValue(item.BundleCardId, out var listing)
                || !listing.Bundle!.IsPublic)
                return BadRequest("One of the listings is no longer available.");
            if (listing.Bundle.UserId == buyerId)
                return BadRequest("You cannot order from your own bundle.");
            if (item.Quantity > listing.Quantity)
                return BadRequest(
                    $"Only {listing.Quantity} of \"{listing.Card!.Name}\" available.");
        }

        var orders = dto.Items
            .GroupBy(i => listings[i.BundleCardId].Bundle!.UserId)
            .Select(group => new Order
            {
                BuyerId = buyerId,
                SellerId = group.Key,
                Status = Pending,
                TotalPrice = group.Sum(i => listings[i.BundleCardId].Price * i.Quantity),
                OrderItems = group.Select(i => new OrderItem
                {
                    CardId = listings[i.BundleCardId].CardId,
                    BundleCardId = i.BundleCardId,
                    Quantity = i.Quantity,
                    Price = listings[i.BundleCardId].Price
                }).ToList()
            })
            .ToList();

        _context.Orders.AddRange(orders);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = $"Created {orders.Count} order(s).",
            Orders = orders.Select(o => new { o.Id, o.SellerId, o.TotalPrice, o.Status })
        });
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = CurrentUserId;
        var orders = await _context.Orders
            .Where(o => o.BuyerId == userId || o.SellerId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.Id,
                o.Status,
                o.TotalPrice,
                o.CreatedAt,
                IsSale = o.SellerId == userId,
                Counterparty = o.SellerId == userId ? o.Buyer!.FullName : o.Seller!.FullName,
                Items = o.OrderItems!.Select(i => new
                {
                    i.Id,
                    i.Quantity,
                    i.Price,
                    Card = new { i.Card!.Id, i.Card.Name, i.Card.Code, i.Card.ImageUrl, i.Card.IsFoil }
                })
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpPut("{orderId}/status")]
    public async Task<IActionResult> UpdateStatus(int orderId, [FromBody] UpdateOrderStatusDto dto)
    {
        var userId = CurrentUserId;
        var order = await _context.Orders
            .Include(o => o.OrderItems!)
                .ThenInclude(i => i.BundleCard)
            .FirstOrDefaultAsync(o => o.Id == orderId
                                      && (o.BuyerId == userId || o.SellerId == userId));
        if (order == null) return NotFound("Order not found.");

        var isSeller = order.SellerId == userId;
        var allowed = isSeller
            ? order.Status switch
            {
                Pending => new[] { Accepted, Cancelled },
                Accepted => new[] { Completed, Cancelled },
                _ => Array.Empty<string>()
            }
            : order.Status switch
            {
                Pending => new[] { Cancelled },
                _ => Array.Empty<string>()
            };

        if (!allowed.Contains(dto.Status))
            return BadRequest(order.Status is Completed or Cancelled
                ? $"Order is already {order.Status.ToLower()}."
                : $"You can only change this order to: {string.Join(", ", allowed)}.");

        order.Status = dto.Status;

        // The trade happened in person — take the sold copies off the listing.
        if (dto.Status == Completed)
        {
            foreach (var item in order.OrderItems!)
            {
                if (item.BundleCard != null)
                    item.BundleCard.Quantity = Math.Max(0, item.BundleCard.Quantity - item.Quantity);
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { order.Id, order.Status });
    }
}
