using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingCardsAPI.Data;
using TradingCardsAPI.DTOs;
using TradingCardsAPI.Models;

namespace TradingCardsAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CollectionController : ControllerBase
{
    private readonly AppDbContext _context;

    public CollectionController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserCollections(int userId)
    {
        var collections = await _context.Collections
            .Where(c => c.UserId == userId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.CreatedAt,
                CardCount = c.Items!.Sum(i => (int?)i.Quantity) ?? 0
            })
            .ToListAsync();

        return Ok(collections);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCollection(int id)
    {
        var collection = await _context.Collections
            .Where(c => c.Id == id)
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
        if (!await _context.Users.AnyAsync(u => u.Id == dto.UserId))
            return BadRequest("User not found.");

        var name = dto.Name.Trim();
        if (name.Length == 0) return BadRequest("Collection name is required.");

        var exists = await _context.Collections
            .AnyAsync(c => c.UserId == dto.UserId && c.Name.ToLower() == name.ToLower());
        if (exists) return BadRequest("You already have a collection with that name.");

        var collection = new Collection { UserId = dto.UserId, Name = name };
        _context.Collections.Add(collection);
        await _context.SaveChangesAsync();

        return Ok(new { collection.Id, collection.Name, collection.CreatedAt, CardCount = 0 });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> RenameCollection(int id, [FromBody] RenameCollectionDto dto)
    {
        var collection = await _context.Collections.FindAsync(id);
        if (collection == null) return NotFound("Collection not found.");

        var name = dto.Name.Trim();
        if (name.Length == 0) return BadRequest("Collection name is required.");

        var exists = await _context.Collections
            .AnyAsync(c => c.UserId == collection.UserId && c.Id != id && c.Name.ToLower() == name.ToLower());
        if (exists) return BadRequest("You already have a collection with that name.");

        collection.Name = name;
        await _context.SaveChangesAsync();
        return Ok(new { collection.Id, collection.Name });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCollection(int id)
    {
        var collection = await _context.Collections.FindAsync(id);
        if (collection == null) return NotFound("Collection not found.");

        _context.Collections.Remove(collection);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Collection deleted." });
    }

    [HttpPut("{id}/cards/{itemId}")]
    public async Task<IActionResult> UpdateCardQuantity(int id, int itemId, [FromBody] UpdateCollectionCardDto dto)
    {
        var item = await _context.CollectionCards
            .FirstOrDefaultAsync(cc => cc.Id == itemId && cc.CollectionId == id);
        if (item == null) return NotFound("Card not found in collection.");

        if (dto.Quantity < 1) return BadRequest("Quantity must be at least 1.");

        item.Quantity = dto.Quantity;
        await _context.SaveChangesAsync();
        return Ok(new { item.Id, item.CollectionId, item.CardId, item.Quantity });
    }

    [HttpDelete("{id}/cards/{itemId}")]
    public async Task<IActionResult> RemoveCardFromCollection(int id, int itemId)
    {
        var item = await _context.CollectionCards
            .FirstOrDefaultAsync(cc => cc.Id == itemId && cc.CollectionId == id);
        if (item == null) return NotFound("Card not found in collection.");

        _context.CollectionCards.Remove(item);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Card removed from collection." });
    }

    [HttpPost("{id}/cards")]
    public async Task<IActionResult> AddCardToCollection(int id, [FromBody] AddCollectionCardDto dto)
    {
        var collection = await _context.Collections.FindAsync(id);
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
}
