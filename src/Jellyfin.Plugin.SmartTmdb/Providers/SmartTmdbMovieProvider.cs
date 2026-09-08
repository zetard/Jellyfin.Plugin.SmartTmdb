using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// Smart TMDB movie similar items provider.
/// </summary>
public sealed class SmartTmdbMovieProvider : IRemoteSimilarItemsProvider<Movie>
{
    private readonly ILogger<SmartTmdbMovieProvider> _logger;
    private readonly ITmdbClient _tmdbClient;
    private readonly ILocalMovieResolver _localResolver;
    private readonly ICandidateScorer _scorer;
    private readonly IPluginSettingsAccessor _settingsAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartTmdbMovieProvider"/> class.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="tmdbClient">TMDB client.</param>
    /// <param name="localResolver">Local resolver.</param>
    /// <param name="scorer">Scorer.</param>
    /// <param name="settingsAccessor">Settings accessor.</param>
    public SmartTmdbMovieProvider(
        ILogger<SmartTmdbMovieProvider> logger,
        ITmdbClient tmdbClient,
        ILocalMovieResolver localResolver,
        ICandidateScorer scorer,
        IPluginSettingsAccessor settingsAccessor)
    {
        _logger = logger;
        _tmdbClient = tmdbClient;
        _localResolver = localResolver;
        _scorer = scorer;
        _settingsAccessor = settingsAccessor;
    }

    /// <inheritdoc/>
    public string Name => "Smart TMDB Recommendations";

    /// <inheritdoc/>
    public MetadataPluginType Type => MetadataPluginType.SimilarityProvider;

    /// <inheritdoc/>
    public TimeSpan? CacheDuration => null;

    /// <inheritdoc/>
    public async IAsyncEnumerable<SimilarItemReference> GetSimilarItemsAsync(
        Movie item,
        SimilarItemsQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield break;
    }
}
