using Microsoft.Extensions.Hosting;

namespace CryptoDataPipeline.Services;

public class BackfillHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackfillHostedService> _logger;
    private Task? _backfillTask;
    private CancellationTokenSource? _cts;

    public BackfillHostedService(IServiceProvider serviceProvider, ILogger<BackfillHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _backfillTask = Task.Run(async () =>
        {
            var ct = _cts.Token;
            try
            {
                await Task.Delay(2000, ct); // Give app time to fully start

                // Run immediately on startup to catch up on any missing data
                await RunBackfill(ct);

                // Then schedule to run every day at midnight UTC
                while (!ct.IsCancellationRequested)
                {
                    var now = DateTime.UtcNow;
                    var nextMidnight = now.Date.AddDays(1);
                    var delay = nextMidnight - now;

                    _logger.LogInformation("Next backfill scheduled at {NextRun} UTC ({Hours:F1} hours from now)",
                        nextMidnight, delay.TotalHours);

                    await Task.Delay(delay, ct);

                    if (!ct.IsCancellationRequested)
                        await RunBackfill(ct);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Backfill scheduler was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backfill scheduler encountered an error");
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    private async Task RunBackfill(CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var backfill = scope.ServiceProvider.GetRequiredService<CoinGeckoService>();

            _logger.LogInformation("Starting cryptocurrency data backfill...");
            await backfill.BackfillAllAsync(ct);
            _logger.LogInformation("Backfill completed successfully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Backfill was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backfill encountered an error (scheduler continues)");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts != null)
            await _cts.CancelAsync();

        if (_backfillTask != null)
        {
            try
            {
                await _backfillTask;
            }
            catch
            {
                // Already logged
            }
        }
    }
}