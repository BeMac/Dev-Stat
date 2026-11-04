namespace DevStats.Websites.Monitoring;

/// <summary>Combines configured websites with their latest check results. Used by the dashboard and the API.</summary>
public sealed class WebsiteStatusService(IWebsiteStore websiteStore, WebsiteCheckResultStore results)
{
    public async Task<IReadOnlyList<WebsiteStatus>> GetAllAsync(CancellationToken ct = default)
    {
        var websites = await websiteStore.GetAllAsync(ct);
        return websites.Select(ToStatus).ToList();
    }

    private WebsiteStatus ToStatus(Website website)
    {
        // Ignore a result for an old URL that the site has since been changed away from.
        var result = results.Get(website.Id) is { } r && r.Url == website.Url ? r : null;

        var state = !website.Enabled ? WebsiteState.Disabled
            : result is null ? WebsiteState.Pending
            : result.IsUp ? WebsiteState.Up
            : WebsiteState.Down;

        return new WebsiteStatus(
            website.Id,
            website.Name,
            website.Url,
            website.Environment,
            state,
            result?.StatusCode,
            result?.ResponseTimeMs,
            result?.Error,
            result?.CheckedAt);
    }
}
