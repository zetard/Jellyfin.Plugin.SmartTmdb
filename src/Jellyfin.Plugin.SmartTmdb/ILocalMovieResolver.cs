using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using Movie = MediaBrowser.Controller.Entities.Movies.Movie;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// Local movie resolver.
/// </summary>
public interface ILocalMovieResolver
{
    /// <summary>
    /// Resolves TMDB IDs to local movies.
    /// </summary>
    /// <param name="tmdbIds">TMDB IDs to resolve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary mapping TMDB ID to local movie.</returns>
    Task<IReadOnlyDictionary<int, Movie>> ResolveAsync(IReadOnlyList<int> tmdbIds, CancellationToken cancellationToken);
}
