using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// Raw TMDB cache.
/// </summary>
public interface IRawTmdbCache
{
    /// <summary>
    /// Gets a cached response.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cached value or null.</returns>
    Task<string?> GetAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Sets a cached response.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="value">Response value.</param>
    /// <param name="ttl">Time to live.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAsync(string key, string value, System.TimeSpan ttl, CancellationToken cancellationToken);
}
