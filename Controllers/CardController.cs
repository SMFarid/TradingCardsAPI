using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingCardsAPI.Data;
using TradingCardsAPI.DTOs;
using TradingCardsAPI.Models;

namespace TradingCardsAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CardController : ControllerBase
{
    private readonly AppDbContext _context;

    public CardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("by-game/{gameId}")]
    public async Task<IActionResult> GetCardsByGame(int gameId)
    {
        var cards = await _context.Cards.Where(c => c.GameId == gameId).ToListAsync();
        return Ok(cards);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCard([FromBody] CardDto dto)
    {
        var card = new Card
        {
            GameId = dto.GameId,
            Name = dto.Name,
            Code = dto.Code,
            Version = dto.Version,
            Rarity = dto.Rarity,
            ImageUrl = dto.ImageUrl
        };

        _context.Cards.Add(card);
        await _context.SaveChangesAsync();
        return Ok(card);
    }
}
