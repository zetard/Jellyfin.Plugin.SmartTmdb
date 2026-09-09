using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using Movie = MediaBrowser.Controller.Entities.Movies.Movie;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// Local movie resolution result.
/// </summary>
/// <param name="Movie">Local movie item.</param>
/// <param name="UserData">User data for the movie, if a user was provided.</param>
public sealed class LocalMovieResolution(Movie Movie, UserItemData? UserData)
{
    /// <summary>
    /// Gets the local movie item.
    /// </summary>
    public Movie Movie { get; } = Movie;

    /// <summary>
    /// Gets the user data for the movie, if a user was provided.
    /// </summary>
    public UserItemData? UserData { get; } = UserData;
}

/// <summary>
/// Local movie resolver.
/// </summary>
public interface ILocalMovieResolver
{
    /// <summary>
    /// Resolves TMDB IDs to local movies and attaches user data when a user is provided.
    /// </summary>
    /// <param name="tmdbIds">TMDB IDs to resolve.</param>
    /// <param name="userId">Optional user ID for watched-state personalization.</param>
    /// <param name="excludeItemIds">Local item IDs requested by Jellyfin to exclude.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary mapping TMDB ID to local movie resolution result.</returns>
    Task<IReadOnlyDictionary<int, LocalMovieResolution>> ResolveAsync(IReadOnlyList<int> tmdbIds, Guid? userId, IReadOnlyList<Guid> excludeItemIds, CancellationToken cancellationToken);
}
