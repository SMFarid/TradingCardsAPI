using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingCardsAPI.Data;
using TradingCardsAPI.DTOs;
using TradingCardsAPI.Models;

namespace TradingCardsAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly AppDbContext _context;

    public StockController(AppDbContext context)
    {
        _context = context;
    }

    // Public: buyers browse a seller's stock without logging in.
    [HttpGet("seller/{userId}")]
    public async Task<IActionResult> GetSellerStock(int userId)
    {
        var stock = await _context.SellerStocks
            .Where(s => s.UserId == userId)
            .Select(s => new
            {
                s.Id,
                s.Quantity,
                s.UserPrice,
                Card = new
                {
                    s.Card!.Id,
                    s.Card.Name,
                    s.Card.Code,
                    s.Card.Version,
                    s.Card.Rarity,
                    s.Card.ImageUrl,
                    s.Card.IsFoil,
                    s.Card.MarketPrice
                }
            })
            .ToListAsync();

        return Ok(stock);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddOrUpdateStock([FromBody] AddStockDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (!await _context.Cards.AnyAsync(c => c.Id == dto.CardId))
            return BadRequest("Card not found.");

        var stock = await _context.SellerStocks
            .FirstOrDefaultAsync(s => s.UserId == userId && s.CardId == dto.CardId);

        if (stock != null)
        {
            stock.Quantity += dto.Quantity;
            stock.UserPrice = dto.Price;
        }
        else
        {
            stock = new SellerStock
            {
                UserId = userId,
                CardId = dto.CardId,
                Quantity = dto.Quantity,
                UserPrice = dto.Price
            };
            _context.SellerStocks.Add(stock);
        }

        await _context.SaveChangesAsync();
        return Ok(new { stock.Id, stock.UserId, stock.CardId, stock.Quantity, stock.UserPrice });
    }
}
