namespace DevStats.Websites;

/// <summary>Persists the list of monitored websites.</summary>
public interface IWebsiteStore
{
    /// <summary>Raised after any add, update or delete.</summary>
    event Action? Changed;

    Task<IReadOnlyList<Website>> GetAllAsync(CancellationToken ct = default);
    Task<Website?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Website> AddAsync(Website website, CancellationToken ct = default);
    Task<bool> UpdateAsync(Website website, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
