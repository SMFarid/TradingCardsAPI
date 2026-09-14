using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingCardsAPI.Data;
using TradingCardsAPI.DTOs;
using TradingCardsAPI.Models;
using TradingCardsAPI.Services;

namespace TradingCardsAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CardController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CardLookupService _lookup;

    public CardController(AppDbContext context, CardLookupService lookup)
    {
        _context = context;
        _lookup = lookup;
    }

    [HttpPost("identify")]
    public async Task<IActionResult> Identify([FromBody] IdentifyCardDto dto)
    {
        if (dto.Lines.Count == 0) return BadRequest("No text to identify.");

        var candidates = await _lookup.IdentifyAsync(dto.Lines);
        if (candidates.Count == 0) return NotFound("Could not identify a card from the scanned text.");

        return Ok(new { Candidates = candidates });
    }

    /// <summary>
    /// Saves the printing the user picked (if not already known) and returns its database id.
    /// </summary>
    [HttpPost("resolve")]
    public async Task<IActionResult> Resolve([FromBody] ResolveCardDto dto)
    {
        var card = await _lookup.ResolveAsync(dto.Game, new Card
        {
            Name = dto.Name,
            Code = dto.Code,
            Version = dto.Version,
            Rarity = dto.Rarity,
            ImageUrl = dto.ImageUrl
        });

        return Ok(new
        {
            card.Id,
            card.Name,
            card.Code,
            card.Version,
            card.Rarity,
            card.ImageUrl,
            Game = card.Game?.Name
        });
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
