using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Movie = MediaBrowser.Controller.Entities.Movies.Movie;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// Local movie resolver implementation using ILibraryManager and IUserDataManager.
/// </summary>
public sealed class LocalMovieResolver : ILocalMovieResolver
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IUserDataManager _userDataManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalMovieResolver"/> class.
    /// </summary>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="userDataManager">User data manager.</param>
    public LocalMovieResolver(ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _userDataManager = userDataManager;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyDictionary<int, LocalMovieResolution>> ResolveAsync(IReadOnlyList<int> tmdbIds, Guid? userId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tmdbIds);

        if (tmdbIds.Count == 0)
        {
            return Task.FromResult<IReadOnlyDictionary<int, LocalMovieResolution>>(new Dictionary<int, LocalMovieResolution>());
        }

        var query = new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Movie },
            HasAnyProviderIds = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase),
        };

        foreach (int tmdbId in tmdbIds)
        {
            query.HasAnyProviderIds["Tmdb"] = query.HasAnyProviderIds.TryGetValue("Tmdb", out string[]? existing)
                ? existing.Concat(new[] { tmdbId.ToString() }).ToArray()
                : new[] { tmdbId.ToString() };
        }

        IReadOnlyList<BaseItem> items = _libraryManager.GetItemList(query);

        Dictionary<int, LocalMovieResolution> resolved = new Dictionary<int, LocalMovieResolution>();
        foreach (BaseItem item in items)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (item is not Movie movie)
            {
                continue;
            }

            if (movie.ProviderIds.TryGetValue("Tmdb", out string? tmdbString) && int.TryParse(tmdbString, out int parsedTmdbId))
            {
                resolved[parsedTmdbId] = new LocalMovieResolution(movie, null);
            }
        }

        if (userId.HasValue && resolved.Count > 0 && _userManager.GetUserById(userId.Value) is User user)
        {
            Dictionary<Guid, UserItemData> userDataMap = _userDataManager.GetUserDataBatch(resolved.Values.Select(r => r.Movie).ToList(), user);
            foreach (KeyValuePair<int, LocalMovieResolution> kvp in resolved)
            {
                if (userDataMap.TryGetValue(kvp.Value.Movie.Id, out UserItemData? userData))
                {
                    resolved[kvp.Key] = new LocalMovieResolution(kvp.Value.Movie, userData);
                }
            }
        }

        return Task.FromResult<IReadOnlyDictionary<int, LocalMovieResolution>>(resolved);
    }
}
