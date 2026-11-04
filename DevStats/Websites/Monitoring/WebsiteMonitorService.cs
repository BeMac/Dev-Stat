using System.Collections.Concurrent;

namespace DevStats.Websites.Monitoring;

/// <summary>
/// Background loop that checks each enabled website on its own interval.
/// Wakes immediately when the website list changes, so new or edited sites are checked right away.
/// </summary>
public sealed class WebsiteMonitorService(
    IWebsiteStore websiteStore,
    WebsiteChecker checker,
    WebsiteCheckResultStore results,
    ILogger<WebsiteMonitorService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly ConcurrentDictionary<Guid, byte> _inFlight = new();
    private readonly SemaphoreSlim _wake = new(0, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        websiteStore.Changed += WakeUp;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await StartDueChecksAsync(stoppingToken);

                try
                {
                    await _wake.WaitAsync(PollInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        finally
        {
            websiteStore.Changed -= WakeUp;
        }
    }

    private void WakeUp()
    {
        try
        {
            _wake.Release();
        }
        catch (SemaphoreFullException)
        {
            // Already signalled.
        }
    }

    private async Task StartDueChecksAsync(CancellationToken ct)
    {
        IReadOnlyList<Website> websites;
        try
        {
            websites = await websiteStore.GetAllAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Could not load the website list");
            return;
        }

        var enabled = websites.Where(w => w.Enabled).ToList();
        results.RemoveAllExcept(enabled.Select(w => w.Id));

        var now = DateTimeOffset.UtcNow;
        foreach (var website in enabled)
        {
            var last = results.Get(website.Id);
            var isDue = last is null
                        || last.Url != website.Url
                        || now - last.CheckedAt >= TimeSpan.FromSeconds(website.CheckIntervalSeconds);

            if (isDue && _inFlight.TryAdd(website.Id, 0))
            {
                _ = RunCheckAsync(website, ct);
            }
        }
    }

    private async Task RunCheckAsync(Website website, CancellationToken ct)
    {
        try
        {
            var result = await checker.CheckAsync(website, ct);
            results.Set(result);

            if (!result.IsUp)
            {
                logger.LogWarning("{Name} ({Url}) is down: {Error}", website.Name, website.Url, result.Error);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // App is shutting down.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Check failed unexpectedly for {Name} ({Url})", website.Name, website.Url);
        }
        finally
        {
            _inFlight.TryRemove(website.Id, out _);
        }
    }
}
