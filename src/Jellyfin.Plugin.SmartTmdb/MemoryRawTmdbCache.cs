using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// In-memory raw TMDB cache with bounded capacity and TTL.
/// </summary>
public sealed class MemoryRawTmdbCache : IRawTmdbCache
{
    private readonly int _maxEntries;
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new();
    private int _nextId;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryRawTmdbCache"/> class.
    /// </summary>
    /// <param name="maxEntries">Maximum cache entries.</param>
    public MemoryRawTmdbCache(int maxEntries = 2000)
    {
        _maxEntries = Math.Max(maxEntries, 1);
    }

    /// <inheritdoc/>
    public Task<string?> GetAsync(string key, CancellationToken cancellationToken)
    {
        if (_entries.TryGetValue(key, out CacheEntry? entry))
        {
            if (DateTimeOffset.UtcNow < entry.ExpiresAt)
            {
                return Task.FromResult<string?>(entry.Value);
            }

            _entries.TryRemove(key, out _);
        }

        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc/>
    public Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        if (_entries.Count >= _maxEntries)
        {
            EvictExpired();

            if (_entries.Count >= _maxEntries)
            {
                EvictOldest();
            }
        }

        CacheEntry entry = new CacheEntry
        {
            Value = value,
            ExpiresAt = DateTimeOffset.UtcNow + ttl,
            InsertOrder = Interlocked.Increment(ref _nextId),
        };

        _entries[key] = entry;
        return Task.CompletedTask;
    }

    private void EvictExpired()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (KeyValuePair<string, CacheEntry> kvp in _entries)
        {
            if (kvp.Value.ExpiresAt <= now)
            {
                _entries.TryRemove(kvp.Key, out _);
            }
        }
    }

    private void EvictOldest()
    {
        string? oldestKey = null;
        long oldestOrder = long.MaxValue;

        foreach (KeyValuePair<string, CacheEntry> kvp in _entries)
        {
            if (kvp.Value.InsertOrder < oldestOrder)
            {
                oldestOrder = kvp.Value.InsertOrder;
                oldestKey = kvp.Key;
            }
        }

        if (oldestKey is not null)
        {
            _entries.TryRemove(oldestKey, out _);
        }
    }

    private sealed class CacheEntry
    {
        public string Value { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
        public long InsertOrder { get; set; }
    }
}