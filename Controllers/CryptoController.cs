using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CryptoDataPipeline.Data;
using CryptoDataPipeline.Models;
using CryptoDataPipeline.Services;

namespace CryptoDataPipeline.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CryptoController : ControllerBase {
    private readonly AppDbContext _context;
    private readonly CoinGeckoService _coinGecko;

    public CryptoController(AppDbContext context, CoinGeckoService coinGecko)
    {
        _context = context;
        _coinGecko = coinGecko;
    }

    [HttpGet("{symbol}")]
    public async Task<IActionResult> GetHistory(string symbol)
    {
        var prices = await _context.CryptoPrices
            .Where(p => p.Symbol.ToLower() == symbol.ToLower())
            .OrderBy(p => p.Date)
            .ToListAsync();

        if (!prices.Any())
            return NotFound($"No data found for symbol: {symbol}");

        return Ok(prices);
    }

    [HttpGet("symbols", Order = 0)]
    public async Task<IActionResult> GetSymbols()
    {
        var symbols = await _context.CryptoPrices
            .Select(p => p.Symbol)
            .Distinct()
            .ToListAsync();
        if (!symbols.Any())
            return NotFound("No data found");
        return Ok(symbols);
    }

    [HttpGet("{symbol}/before")]
    public async Task<IActionResult> GetBefore(string symbol, [FromQuery] DateOnly to)
    {
        var prices = await _context.CryptoPrices
            .Where(p => p.Symbol.ToLower() == symbol.ToLower() && p.Date < to)
            .OrderBy(p => p.Date)
            .ToListAsync();

        if (!prices.Any())
            return NotFound($"No data found for symbol: {symbol} before {to:yyyy-MM-dd}");

        return Ok(prices);
    }

    [HttpGet("{symbol}/after")]
    public async Task<IActionResult> GetAfter(string symbol, [FromQuery] DateOnly from)
    {
        var prices = await _context.CryptoPrices
            .Where(p => p.Symbol.ToLower() == symbol.ToLower() && p.Date > from)
            .OrderBy(p => p.Date)
            .ToListAsync();

        if (!prices.Any())
            return NotFound($"No data found for symbol: {symbol} after {from:yyyy-MM-dd}");

        return Ok(prices);
    }

    [HttpGet("{symbol}/between")]
    public async Task<IActionResult> GetBetween(string symbol, [FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        var prices = await _context.CryptoPrices
            .Where(p => p.Symbol.ToLower() == symbol.ToLower() && p.Date > from && p.Date < to)
            .OrderBy(p => p.Date)
            .ToListAsync();

        if (!prices.Any())
            return NotFound($"No data found for symbol: {symbol} between {from:yyyy-MM-dd} and {to:yyyy-MM-dd}");

        return Ok(prices);
    }

    [HttpGet("{symbol}/latest")]
    public async Task<IActionResult> GetLatest(string symbol)
    {
        var price = await _context.CryptoPrices
            .Where(p => p.Symbol.ToLower() == symbol.ToLower())
            .OrderByDescending(p => p.Date)
            .FirstOrDefaultAsync();

        if (price == null)
            return NotFound($"No data found for symbol: {symbol}");

        return Ok(price);
    }
}