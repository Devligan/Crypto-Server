using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using CryptoDataPipeline.Data;
using CryptoDataPipeline.Models;

namespace CryptoDataPipeline.Services;

public class CoinGeckoService
{
    private readonly HttpClient _http;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly string _apiKey;
    private readonly ILogger<CoinGeckoService> _logger;

    private static readonly (string CoinId, string Symbol)[] Coins =
    [
        ("bitcoin",  "BTC"),
        ("ethereum", "ETH"),
        ("solana",   "SOL"),
        ("ripple",   "XRP"),
        ("usd-coin", "USDC"),
        ("dogecoin", "DOGE"),
        ("cardano",  "ADA"),
    ];

    private static readonly TimeSpan CallDelay = TimeSpan.FromMilliseconds(2500);

    public CoinGeckoService(HttpClient http, IDbContextFactory<AppDbContext> dbFactory, IConfiguration config, ILogger<CoinGeckoService> logger)
    {
        _http = http;
        _dbFactory = dbFactory;
        _logger = logger;
        _apiKey = config["CoinGecko:ApiKey"] ?? "";
        _http.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
        _http.DefaultRequestHeaders.Add("x-cg-demo-api-key", _apiKey);
    }

    public async Task BackfillAllAsync(CancellationToken ct = default)
    {
        foreach (var (coinId, symbol) in Coins)
        {
            if (ct.IsCancellationRequested) break;
            _logger.LogInformation("Starting backfill for {Symbol}", symbol);
            await BackfillCoinAsync(coinId, symbol, ct);
        }
    }

    private async Task BackfillCoinAsync(string coinId, string symbol, CancellationToken ct)
    {
        if (!await HasCreditsAsync())
        {
            _logger.LogWarning("No API credits remaining, stopping backfill.");
            return;
        }

        using var db = await _dbFactory.CreateDbContextAsync(ct);

        var newest = await db.CryptoPrices
            .Where(p => p.Symbol == symbol)
            .MaxAsync(p => (DateOnly?)p.Date, ct);

        var oldest = await db.CryptoPrices
            .Where(p => p.Symbol == symbol)
            .MinAsync(p => (DateOnly?)p.Date, ct);

        if (newest == null)
        {
            _logger.LogInformation("{Symbol}: DB empty, fetching from newest backwards.", symbol);
            await FetchBackwardsAsync(coinId, symbol, DateOnly.FromDateTime(DateTime.UtcNow), ct);
        }
        else
        {
            var daysSinceNewest = (DateTime.UtcNow.Date - newest.Value.ToDateTime(TimeOnly.MinValue)).Days;
            if (daysSinceNewest > 0)
            {
                _logger.LogInformation("{Symbol}: Filling {Days} days forward.", symbol, daysSinceNewest);
                await FetchAndStoreRangeAsync(coinId, symbol, daysSinceNewest, ct);
            }

            if (!ct.IsCancellationRequested && await HasCreditsAsync())
            {
                _logger.LogInformation("{Symbol}: Fetching backwards from {Date}", symbol, oldest!.Value);
                await FetchBackwardsAsync(coinId, symbol, oldest!.Value, ct);
            }
        }
    }

    private async Task FetchBackwardsAsync(string coinId, string symbol, DateOnly from, CancellationToken ct)
    {
        var cursor = from.ToDateTime(TimeOnly.MinValue);

        while (!ct.IsCancellationRequested)
        {
            if (!await HasCreditsAsync())
            {
                _logger.LogWarning("Out of API credits, stopping.");
                break;
            }

            var daysBack = Math.Min(90, (int)(cursor - new DateTime(2010, 1, 1)).TotalDays);
            if (daysBack <= 0) break;

            _logger.LogInformation("{Symbol}: Fetching {Days} days back from {Date}", symbol, daysBack, cursor);
            int saved = await FetchAndStoreRangeAsync(coinId, symbol, daysBack, ct);

            if (saved == 0) break;

            cursor = cursor.AddDays(-daysBack);
            await Task.Delay(CallDelay, ct);
        }
    }

    private async Task<int> FetchAndStoreRangeAsync(string coinId, string symbol, int days, CancellationToken ct)
    {
        try
        {
            await Task.Delay(CallDelay, ct);
            var chartRes = await _http.GetStringAsync($"coins/{coinId}/market_chart?vs_currency=usd&days={days}&interval=daily", ct);
            var chart = JsonSerializer.Deserialize<JsonElement>(chartRes)!;

            var prices = chart.GetProperty("prices").EnumerateArray()
                .GroupBy(p => DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeMilliseconds(p[0].GetInt64()).UtcDateTime))
                .ToDictionary(g => g.Key, g => g.Last()[1].GetDecimal());

            var volumes = chart.GetProperty("total_volumes").EnumerateArray()
                .GroupBy(v => DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeMilliseconds(v[0].GetInt64()).UtcDateTime))
                .ToDictionary(g => g.Key, g => g.Last()[1].GetDecimal());

            using var db = await _dbFactory.CreateDbContextAsync(ct);
            int saved = 0;

            foreach (var date in prices.Keys)
            {
                var exists = await db.CryptoPrices.AnyAsync(p => p.Symbol == symbol && p.Date == date, ct);
                if (exists) continue;

                db.CryptoPrices.Add(new CryptoPrice
                {
                    Symbol = symbol,
                    Date = date,
                    Close = prices[date],
                    Volume = volumes.TryGetValue(date, out var vol) ? vol : 0
                });
                saved++;
            }

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("{Symbol}: Saved {Count} new rows.", symbol, saved);
            return saved;
        }
        catch (HttpRequestException ex) when ((int?)ex.StatusCode == 429)
        {
            _logger.LogWarning("Rate limited (429), waiting 60 seconds...");
            await Task.Delay(TimeSpan.FromSeconds(60), ct);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching {Symbol}", symbol);
            return 0;
        }
    }

    private async Task<bool> HasCreditsAsync()
    {
        try
        {
            var res = await _http.GetStringAsync("key");
            var json = JsonSerializer.Deserialize<JsonElement>(res);
            var remaining = json.GetProperty("plan_attributes").GetProperty("monthly_call_credit").GetInt32();
            var used = json.GetProperty("current_month_daily_calls").GetInt32();
            _logger.LogInformation("API credits: {Used}/{Total}", used, remaining);
            return used < remaining;
        }
        catch
        {
            return true;
        }
    }
}