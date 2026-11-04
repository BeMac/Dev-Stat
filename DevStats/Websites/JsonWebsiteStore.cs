using System.Text.Json;
using DevStats.Storage;
using Microsoft.Extensions.Options;

namespace DevStats.Websites;

/// <summary>Stores websites in a JSON file (no database). Safe for concurrent use within one app instance.</summary>
public sealed class JsonWebsiteStore : IWebsiteStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string _filePath;
    private List<Website>? _cache;

    public JsonWebsiteStore(IOptions<StorageOptions> options, IHostEnvironment env)
    {
        var dataDirectory = Path.Combine(env.ContentRootPath, options.Value.DataDirectory);
        _filePath = Path.Combine(dataDirectory, "websites.json");
    }

    public event Action? Changed;

    public async Task<IReadOnlyList<Website>> GetAllAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var websites = await LoadAsync(ct);
            return websites
                .OrderBy(w => w.Name, StringComparer.OrdinalIgnoreCase)
                .Select(w => w.Clone())
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Website?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var websites = await LoadAsync(ct);
            return websites.FirstOrDefault(w => w.Id == id)?.Clone();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Website> AddAsync(Website website, CancellationToken ct = default)
    {
        var created = website.Clone();
        created.Id = Guid.NewGuid();

        await MutateAsync(list => { list.Add(created); return true; }, ct);
        return created.Clone();
    }

    public Task<bool> UpdateAsync(Website website, CancellationToken ct = default)
    {
        var updated = website.Clone();
        return MutateAsync(list =>
        {
            var index = list.FindIndex(w => w.Id == updated.Id);
            if (index < 0) return false;
            list[index] = updated;
            return true;
        }, ct);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) =>
        MutateAsync(list => list.RemoveAll(w => w.Id == id) > 0, ct);

    /// <summary>Applies a change to a copy of the list, saves it, and only then swaps it into the cache.</summary>
    private async Task<bool> MutateAsync(Func<List<Website>, bool> change, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var working = new List<Website>(await LoadAsync(ct));
            if (!change(working)) return false;

            await SaveAsync(working, ct);
            _cache = working;
        }
        finally
        {
            _lock.Release();
        }

        Changed?.Invoke();
        return true;
    }

    private async Task<List<Website>> LoadAsync(CancellationToken ct)
    {
        if (_cache is not null) return _cache;

        if (!File.Exists(_filePath))
        {
            _cache = [];
            return _cache;
        }

        // A malformed file throws here rather than being silently overwritten.
        await using var stream = File.OpenRead(_filePath);
        var file = await JsonSerializer.DeserializeAsync<WebsitesFile>(stream, JsonOptions, ct);
        _cache = file?.Websites ?? [];
        return _cache;
    }

    private async Task SaveAsync(List<Website> websites, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        // Write to a temp file then swap, so a crash mid-write can't corrupt the real file.
        var tempPath = _filePath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, new WebsitesFile { Websites = websites }, JsonOptions, ct);
        }

        File.Move(tempPath, _filePath, overwrite: true);
    }

    private sealed class WebsitesFile
    {
        public List<Website> Websites { get; set; } = [];
    }
}
