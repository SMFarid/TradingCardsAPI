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

    [HttpGet("seller/{userId}")]
    public async Task<IActionResult> GetSellerStock(int userId)
    {
        var stock = await _context.SellerStocks
            .Include(s => s.Card)
            .Where(s => s.UserId == userId)
            .ToListAsync();
            
        return Ok(stock);
    }

    [HttpPost]
    public async Task<IActionResult> AddOrUpdateStock([FromBody] AddStockDto dto)
    {
        var stock = await _context.SellerStocks
            .FirstOrDefaultAsync(s => s.UserId == dto.UserId && s.CardId == dto.CardId);

        if (stock != null)
        {
            stock.Quantity += dto.Quantity;
            stock.UserPrice = dto.Price;
        }
        else
        {
            stock = new SellerStock
            {
                UserId = dto.UserId,
                CardId = dto.CardId,
                Quantity = dto.Quantity,
                UserPrice = dto.Price
            };
            _context.SellerStocks.Add(stock);
        }

        await _context.SaveChangesAsync();
        return Ok(stock);
    }
}
