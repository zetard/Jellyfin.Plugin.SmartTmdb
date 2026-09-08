using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// In-memory raw TMDB cache.
/// </summary>
public sealed class MemoryRawTmdbCache : IRawTmdbCache
{
    /// <inheritdoc/>
    public Task<string?> GetAsync(string key, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc/>
    public Task SetAsync(string key, string value, System.TimeSpan ttl, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
