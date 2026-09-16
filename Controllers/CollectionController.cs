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
public class CollectionController : ControllerBase
{
    private readonly AppDbContext _context;

    public CollectionController(AppDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetMyCollections()
    {
        var userId = CurrentUserId;
        var collections = await _context.Collections
            .Where(c => c.UserId == userId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.CreatedAt,
                CardCount = c.Items!.Sum(i => (int?)i.Quantity) ?? 0,
                TotalValue = c.Items!.Sum(i => (decimal?)(i.Quantity * (i.Card!.MarketPrice ?? 0))) ?? 0
            })
            .ToListAsync();

        return Ok(collections);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCollection(int id)
    {
        var userId = CurrentUserId;
        var collection = await _context.Collections
            .Where(c => c.Id == id && c.UserId == userId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.CreatedAt,
                Items = c.Items!.Select(i => new
                {
                    i.Id,
                    i.Quantity,
                    Card = new
                    {
                        i.Card!.Id,
                        i.Card.Name,
                        i.Card.Code,
                        i.Card.Version,
                        i.Card.Rarity,
                        i.Card.ImageUrl,
                        i.Card.IsFoil,
                        i.Card.MarketPrice,
                        Game = i.Card.Game!.Name
                    }
                })
            })
            .FirstOrDefaultAsync();

        if (collection == null) return NotFound("Collection not found.");
        return Ok(collection);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCollection([FromBody] CreateCollectionDto dto)
    {
        var userId = CurrentUserId;
        var name = dto.Name.Trim();
        if (name.Length == 0) return BadRequest("Collection name is required.");

        var exists = await _context.Collections
            .AnyAsync(c => c.UserId == userId && c.Name.ToLower() == name.ToLower());
        if (exists) return BadRequest("You already have a collection with that name.");

        var collection = new Collection { UserId = userId, Name = name };
        _context.Collections.Add(collection);
        await _context.SaveChangesAsync();

        return Ok(new { collection.Id, collection.Name, collection.CreatedAt, CardCount = 0, TotalValue = 0m });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> RenameCollection(int id, [FromBody] RenameCollectionDto dto)
    {
        var userId = CurrentUserId;
        var collection = await _context.Collections
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (collection == null) return NotFound("Collection not found.");

        var name = dto.Name.Trim();
        if (name.Length == 0) return BadRequest("Collection name is required.");

        var exists = await _context.Collections
            .AnyAsync(c => c.UserId == userId && c.Id != id && c.Name.ToLower() == name.ToLower());
        if (exists) return BadRequest("You already have a collection with that name.");

        collection.Name = name;
        await _context.SaveChangesAsync();
        return Ok(new { collection.Id, collection.Name });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCollection(int id)
    {
        var userId = CurrentUserId;
        var collection = await _context.Collections
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (collection == null) return NotFound("Collection not found.");

        _context.Collections.Remove(collection);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Collection deleted." });
    }

    [HttpPost("{id}/cards")]
    public async Task<IActionResult> AddCardToCollection(int id, [FromBody] AddCollectionCardDto dto)
    {
        var userId = CurrentUserId;
        var collection = await _context.Collections
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (collection == null) return NotFound("Collection not found.");

        if (!await _context.Cards.AnyAsync(c => c.Id == dto.CardId))
            return BadRequest("Card not found.");

        if (dto.Quantity < 1) return BadRequest("Quantity must be at least 1.");

        var item = await _context.CollectionCards
            .FirstOrDefaultAsync(cc => cc.CollectionId == id && cc.CardId == dto.CardId);

        if (item != null)
        {
            item.Quantity += dto.Quantity;
        }
        else
        {
            item = new CollectionCard { CollectionId = id, CardId = dto.CardId, Quantity = dto.Quantity };
            _context.CollectionCards.Add(item);
        }

        await _context.SaveChangesAsync();
        return Ok(new { item.Id, item.CollectionId, item.CardId, item.Quantity });
    }

    [HttpPut("{id}/cards/{itemId}")]
    public async Task<IActionResult> UpdateCardQuantity(int id, int itemId, [FromBody] UpdateCollectionCardDto dto)
    {
        var userId = CurrentUserId;
        var item = await _context.CollectionCards
            .FirstOrDefaultAsync(cc =>
                cc.Id == itemId && cc.CollectionId == id && cc.Collection!.UserId == userId);
        if (item == null) return NotFound("Card not found in collection.");

        if (dto.Quantity < 1) return BadRequest("Quantity must be at least 1.");

        item.Quantity = dto.Quantity;
        await _context.SaveChangesAsync();
        return Ok(new { item.Id, item.CollectionId, item.CardId, item.Quantity });
    }

    [HttpDelete("{id}/cards/{itemId}")]
    public async Task<IActionResult> RemoveCardFromCollection(int id, int itemId)
    {
        var userId = CurrentUserId;
        var item = await _context.CollectionCards
            .FirstOrDefaultAsync(cc =>
                cc.Id == itemId && cc.CollectionId == id && cc.Collection!.UserId == userId);
        if (item == null) return NotFound("Card not found in collection.");

        _context.CollectionCards.Remove(item);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Card removed from collection." });
    }
}
