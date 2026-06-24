using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingCardsAPI.Data;
using TradingCardsAPI.DTOs;
using TradingCardsAPI.Models;

namespace TradingCardsAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly AppDbContext _context;

    public GameController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetGames()
    {
        var games = await _context.Games.ToListAsync();
        return Ok(games);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGame([FromBody] GameDto dto)
    {
        var game = new Game { Name = dto.Name };
        _context.Games.Add(game);
        await _context.SaveChangesAsync();
        return Ok(game);
    }
}
