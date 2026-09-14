using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TradingCardsAPI.Data;
using TradingCardsAPI.Models;

namespace TradingCardsAPI.Services;

/// <summary>
/// A possible identification for a scanned card. CardId is set only for cards
/// already in the internal database; external candidates get an id when the
/// user confirms one (see CardController.Resolve).
/// </summary>
public record CardCandidate(
    int? CardId,
    string Game,
    string Name,
    string Code,
    string Version,
    string Rarity,
    string ImageUrl,
    string Source);

/// <summary>
/// Identifies a card from OCR text lines. Checks the internal Cards table first,
/// then queries free public TCG APIs (Scryfall for MTG, digimoncard.io for Digimon),
/// returning multiple candidates so the user can pick the right printing / alt art.
/// </summary>
public class CardLookupService
{
    public const string MtgGameName = "Magic: The Gathering";
    public const string DigimonGameName = "Digimon Card Game";
    private const int MaxCandidates = 12;

    // Digimon codes: BT1-010, ST1-03, EX1-001, RB1-004, LM-020, P-001
    private static readonly Regex DigimonCodeRegex =
        new(@"^(?:BT|ST|EX|RB|LM)\d{0,2}-\d{2,3}$|^P-\d{2,3}$", RegexOptions.IgnoreCase);

    private readonly AppDbContext _context;
    private readonly HttpClient _http;

    public CardLookupService(AppDbContext context, HttpClient http)
    {
        _context = context;
        _http = http;
    }

    public async Task<List<CardCandidate>> IdentifyAsync(List<string> lines)
    {
        var cleaned = lines
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        var tokens = cleaned
            .SelectMany(l => l.Split(' ', '\t'))
            .Select(t => t.Trim('.', ',', ':', ';', '[', ']', '(', ')'))
            .Where(t => t.Length > 0)
            .ToList();

        // Digimon card codes are distinctive; if one is present this is a Digimon card.
        var digimonCode = tokens.FirstOrDefault(t => DigimonCodeRegex.IsMatch(t));
        if (digimonCode != null)
        {
            var digimon = await DigimonCandidatesAsync(digimonCode.ToUpperInvariant());
            if (digimon.Count > 0) return digimon;
        }

        return await MtgCandidatesAsync(cleaned);
    }

    /// <summary>Finds or creates the card a user picked, so it gets a database id.</summary>
    public async Task<Card> ResolveAsync(string gameName, Card card)
    {
        var game = await _context.Games.FirstOrDefaultAsync(g => g.Name == gameName);
        if (game == null)
        {
            game = new Game { Name = gameName };
            _context.Games.Add(game);
        }

        var existing = await _context.Cards
            .Include(c => c.Game)
            .FirstOrDefaultAsync(c => c.Code == card.Code && c.Game!.Name == gameName);
        if (existing != null) return existing;

        card.Game = game;
        _context.Cards.Add(card);
        await _context.SaveChangesAsync();
        return card;
    }

    private async Task<List<CardCandidate>> DigimonCandidatesAsync(string code)
    {
        var candidates = new List<CardCandidate>();

        var internalMatches = await _context.Cards
            .Include(c => c.Game)
            .Where(c => c.Code == code && c.Game!.Name == DigimonGameName)
            .ToListAsync();
        candidates.AddRange(internalMatches.Select(c => ToCandidate(c, "internal")));

        try
        {
            var byCode = await GetDigimonAsync($"card={Uri.EscapeDataString(code)}");
            candidates.AddRange(byCode);

            // Other printings / alt arts share the card's name but carry different codes.
            var matchedName = byCode.FirstOrDefault()?.Name
                ?? internalMatches.FirstOrDefault()?.Name;
            if (matchedName != null)
            {
                var byName = await GetDigimonAsync($"n={Uri.EscapeDataString(matchedName)}");
                candidates.AddRange(byName.Where(c =>
                    string.Equals(c.Name, matchedName, StringComparison.OrdinalIgnoreCase)));
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            // External API unavailable — internal matches (if any) still count.
        }

        return Dedupe(candidates);
    }

    private async Task<List<CardCandidate>> GetDigimonAsync(string query)
    {
        var response = await _http.GetAsync($"https://digimoncard.io/api-public/search.php?{query}");
        if (!response.IsSuccessStatusCode) return new List<CardCandidate>();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (doc.RootElement.ValueKind != JsonValueKind.Array) return new List<CardCandidate>();

        var results = new List<CardCandidate>();
        foreach (var entry in doc.RootElement.EnumerateArray())
        {
            var cardCode = entry.GetProperty("id").GetString();
            var name = entry.GetProperty("name").GetString();
            if (cardCode == null || name == null) continue;

            var setName = entry.TryGetProperty("set_name", out var sets)
                          && sets.ValueKind == JsonValueKind.Array && sets.GetArrayLength() > 0
                ? sets[0].GetString() ?? ""
                : "";

            results.Add(new CardCandidate(
                null,
                DigimonGameName,
                name,
                cardCode,
                setName,
                entry.TryGetProperty("rarity", out var r) ? r.GetString() ?? "" : "",
                $"https://images.digimoncard.io/images/cards/{cardCode}.jpg",
                "digimoncard.io"));
        }
        return results;
    }

    private async Task<List<CardCandidate>> MtgCandidatesAsync(List<string> lines)
    {
        foreach (var candidate in NameCandidates(lines))
        {
            try
            {
                var response = await _http.GetAsync(
                    $"https://api.scryfall.com/cards/named?fuzzy={Uri.EscapeDataString(candidate)}");
                if (!response.IsSuccessStatusCode) continue;

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var root = doc.RootElement;
                var matchedName = root.GetProperty("name").GetString() ?? candidate;

                var candidates = new List<CardCandidate>();

                var internalMatches = await _context.Cards
                    .Include(c => c.Game)
                    .Where(c => c.Name.ToLower() == matchedName.ToLower()
                                && c.Game!.Name == MtgGameName)
                    .ToListAsync();
                candidates.AddRange(internalMatches.Select(c => ToCandidate(c, "internal")));

                // All printings of this card (different sets and alternative arts).
                var printsUri = root.TryGetProperty("prints_search_uri", out var pu)
                    ? pu.GetString()
                    : null;
                if (printsUri != null)
                    candidates.AddRange(await GetScryfallPrintsAsync(printsUri));
                else
                    candidates.Add(ScryfallToCandidate(root));

                var deduped = Dedupe(candidates);
                if (deduped.Count > 0) return deduped;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
            {
                // Try the next candidate line.
            }
        }

        return new List<CardCandidate>();
    }

    private async Task<List<CardCandidate>> GetScryfallPrintsAsync(string printsUri)
    {
        var response = await _http.GetAsync(printsUri);
        if (!response.IsSuccessStatusCode) return new List<CardCandidate>();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (!doc.RootElement.TryGetProperty("data", out var data)
            || data.ValueKind != JsonValueKind.Array)
            return new List<CardCandidate>();

        return data.EnumerateArray()
            .Take(MaxCandidates)
            .Select(ScryfallToCandidate)
            .ToList();
    }

    private static CardCandidate ScryfallToCandidate(JsonElement card)
    {
        var set = card.GetProperty("set").GetString()?.ToUpperInvariant() ?? "";
        var collectorNumber = card.GetProperty("collector_number").GetString() ?? "";

        // Double-faced cards keep their images under card_faces instead.
        string imageUrl = "";
        if (card.TryGetProperty("image_uris", out var images))
            imageUrl = images.GetProperty("normal").GetString() ?? "";
        else if (card.TryGetProperty("card_faces", out var faces) && faces.GetArrayLength() > 0
                 && faces[0].TryGetProperty("image_uris", out var faceImages))
            imageUrl = faceImages.GetProperty("normal").GetString() ?? "";

        return new CardCandidate(
            null,
            MtgGameName,
            card.GetProperty("name").GetString() ?? "",
            $"{set}-{collectorNumber}",
            card.TryGetProperty("set_name", out var sn) ? sn.GetString() ?? "" : "",
            card.TryGetProperty("rarity", out var r) ? r.GetString() ?? "" : "",
            imageUrl,
            "scryfall");
    }

    private static CardCandidate ToCandidate(Card card, string source) =>
        new(card.Id, card.Game?.Name ?? "", card.Name, card.Code,
            card.Version, card.Rarity, card.ImageUrl, source);

    /// <summary>Removes duplicate printings, keeping the first occurrence (internal matches come first).</summary>
    private static List<CardCandidate> Dedupe(List<CardCandidate> candidates)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return candidates
            .Where(c => seen.Add($"{c.Game}|{c.Code}"))
            .Take(MaxCandidates)
            .ToList();
    }

    private static IEnumerable<string> NameCandidates(List<string> lines)
    {
        return lines
            .Where(l => l.Length is >= 3 and <= 40)
            .Where(l => l.Any(char.IsLetter))
            .Where(l => !l.Contains('/'))
            .Where(l => !DigimonCodeRegex.IsMatch(l))
            .Take(4);
    }
}
