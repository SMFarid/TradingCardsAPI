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
public class BundleController : ControllerBase
{
    private readonly AppDbContext _context;

    public BundleController(AppDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private int? CurrentUserIdOrNull =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyBundles()
    {
        var userId = CurrentUserId;
        var bundles = await _context.Bundles
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.IsPublic,
                b.CreatedAt,
                CardCount = b.Items!.Sum(i => (int?)i.Quantity) ?? 0,
                TotalValue = b.Items!.Sum(i => (decimal?)(i.Quantity * i.Price)) ?? 0
            })
            .ToListAsync();

        return Ok(bundles);
    }

    [AllowAnonymous]
    [HttpGet("browse")]
    public async Task<IActionResult> Browse()
    {
        var userId = CurrentUserIdOrNull;
        var bundles = await _context.Bundles
            .Where(b => b.IsPublic && (userId == null || b.UserId != userId))
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.CreatedAt,
                Seller = b.User!.FullName,
                SellerId = b.UserId,
                CardCount = b.Items!.Sum(i => (int?)i.Quantity) ?? 0,
                TotalValue = b.Items!.Sum(i => (decimal?)(i.Quantity * i.Price)) ?? 0,
                PreviewImages = b.Items!
                    .OrderBy(i => i.Id)
                    .Select(i => i.Card!.ImageUrl)
                    .Take(4)
                    .ToList()
            })
            .ToListAsync();

        return Ok(bundles);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBundle(int id)
    {
        var userId = CurrentUserId;
        var bundle = await _context.Bundles
            .Where(b => b.Id == id && (b.UserId == userId || b.IsPublic))
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.IsPublic,
                b.CreatedAt,
                Seller = b.User!.FullName,
                IsMine = b.UserId == userId,
                Items = b.Items!.Select(i => new
                {
                    i.Id,
                    i.Price,
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

        if (bundle == null) return NotFound("Bundle not found.");
        return Ok(bundle);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBundle([FromBody] CreateBundleDto dto)
    {
        var userId = CurrentUserId;
        var name = dto.Name.Trim();
        if (name.Length == 0) return BadRequest("Bundle name is required.");

        var bundle = new Bundle { UserId = userId, Name = name };
        _context.Bundles.Add(bundle);
        await _context.SaveChangesAsync();

        return Ok(new { bundle.Id, bundle.Name, bundle.IsPublic, bundle.CreatedAt, CardCount = 0, TotalValue = 0m });
    }

    /// <summary>
    /// Copies the cards of the given collections into the bundle. Prices default
    /// to the card's market price; duplicate cards merge quantities. Collections
    /// themselves are not modified.
    /// </summary>
    [HttpPost("{id}/import")]
    public async Task<IActionResult> ImportCollections(int id, [FromBody] ImportCollectionsDto dto)
    {
        var userId = CurrentUserId;
        var bundle = await _context.Bundles
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        if (bundle == null) return NotFound("Bundle not found.");

        var collectionItems = await _context.CollectionCards
            .Include(cc => cc.Card)
            .Where(cc => dto.CollectionIds.Contains(cc.CollectionId)
                         && cc.Collection!.UserId == userId)
            .ToListAsync();

        if (collectionItems.Count == 0)
            return BadRequest("No cards found in the selected collections.");

        var existingByCard = bundle.Items!.ToDictionary(i => i.CardId);
        var added = 0;
        foreach (var item in collectionItems)
        {
            if (existingByCard.TryGetValue(item.CardId, out var bundleCard))
            {
                bundleCard.Quantity += item.Quantity;
            }
            else
            {
                bundleCard = new BundleCard
                {
                    BundleId = bundle.Id,
                    CardId = item.CardId,
                    Quantity = item.Quantity,
                    Price = item.Card!.MarketPrice ?? 0
                };
                _context.BundleCards.Add(bundleCard);
                existingByCard[item.CardId] = bundleCard;
                added++;
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Imported {collectionItems.Count} entries ({added} new cards)." });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBundle(int id, [FromBody] UpdateBundleDto dto)
    {
        var userId = CurrentUserId;
        var bundle = await _context.Bundles
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        if (bundle == null) return NotFound("Bundle not found.");

        if (dto.Name != null)
        {
            var name = dto.Name.Trim();
            if (name.Length == 0) return BadRequest("Bundle name is required.");
            bundle.Name = name;
        }
        if (dto.IsPublic != null) bundle.IsPublic = dto.IsPublic.Value;

        await _context.SaveChangesAsync();
        return Ok(new { bundle.Id, bundle.Name, bundle.IsPublic });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBundle(int id)
    {
        var userId = CurrentUserId;
        var bundle = await _context.Bundles
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        if (bundle == null) return NotFound("Bundle not found.");

        _context.Bundles.Remove(bundle);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Bundle deleted." });
    }

    [HttpPut("{id}/cards/{itemId}")]
    public async Task<IActionResult> UpdateBundleCard(int id, int itemId, [FromBody] UpdateBundleCardDto dto)
    {
        var userId = CurrentUserId;
        var item = await _context.BundleCards
            .FirstOrDefaultAsync(bc =>
                bc.Id == itemId && bc.BundleId == id && bc.Bundle!.UserId == userId);
        if (item == null) return NotFound("Card not found in bundle.");

        if (dto.Price != null)
        {
            if (dto.Price < 0) return BadRequest("Price cannot be negative.");
            item.Price = dto.Price.Value;
        }
        if (dto.Quantity != null)
        {
            if (dto.Quantity < 1) return BadRequest("Quantity must be at least 1.");
            item.Quantity = dto.Quantity.Value;
        }

        await _context.SaveChangesAsync();
        return Ok(new { item.Id, item.BundleId, item.CardId, item.Price, item.Quantity });
    }

    [HttpDelete("{id}/cards/{itemId}")]
    public async Task<IActionResult> RemoveBundleCard(int id, int itemId)
    {
        var userId = CurrentUserId;
        var item = await _context.BundleCards
            .FirstOrDefaultAsync(bc =>
                bc.Id == itemId && bc.BundleId == id && bc.Bundle!.UserId == userId);
        if (item == null) return NotFound("Card not found in bundle.");

        _context.BundleCards.Remove(item);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Card removed from bundle." });
    }
}
