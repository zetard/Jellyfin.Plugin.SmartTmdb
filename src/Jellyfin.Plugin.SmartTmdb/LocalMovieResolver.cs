using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using Movie = MediaBrowser.Controller.Entities.Movies.Movie;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// Local movie resolver skeleton.
/// </summary>
public sealed class LocalMovieResolver : ILocalMovieResolver
{
    /// <inheritdoc/>
    public Task<IReadOnlyDictionary<int, Movie>> ResolveAsync(IReadOnlyList<int> tmdbIds, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyDictionary<int, Movie>>(new Dictionary<int, Movie>());
    }
}
