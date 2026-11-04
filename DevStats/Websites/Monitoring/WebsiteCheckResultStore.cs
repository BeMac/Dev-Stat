using System.Collections.Concurrent;

namespace DevStats.Websites.Monitoring;

/// <summary>In-memory latest check result per website. Results reset when the app restarts.</summary>
public sealed class WebsiteCheckResultStore
{
    private readonly ConcurrentDictionary<Guid, WebsiteCheckResult> _results = new();

    /// <summary>Raised whenever a result is added or removed.</summary>
    public event Action? Changed;

    public WebsiteCheckResult? Get(Guid websiteId) =>
        _results.GetValueOrDefault(websiteId);

    public void Set(WebsiteCheckResult result)
    {
        _results[result.WebsiteId] = result;
        Changed?.Invoke();
    }

    /// <summary>Drops results for websites that were deleted or disabled.</summary>
    public void RemoveAllExcept(IEnumerable<Guid> websiteIds)
    {
        var keep = websiteIds.ToHashSet();
        var removed = false;

        foreach (var id in _results.Keys)
        {
            if (!keep.Contains(id) && _results.TryRemove(id, out _)) removed = true;
        }

        if (removed) Changed?.Invoke();
    }
}
