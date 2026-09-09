using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SmartTmdb.Configuration;
using Jellyfin.Plugin.SmartTmdb.Tmdb;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// Remote similar-items provider for movies that augments Jellyfin's default TMDB results
/// with re-ranked candidates from this plugin's scoring engine.
/// </summary>
public sealed class SmartTmdbMovieProvider : IRemoteSimilarItemsProvider<Movie>
{
    private const string ProviderName = "Smart TMDB Recommendations";
    private const int MaxCandidatePool = 200;
    private const int DefaultResultLimit = 50;
    private const int MaxPages = 5;

    private readonly ITmdbClient _tmdbClient;
    private readonly ILocalMovieResolver _localMovieResolver;
    private readonly ICandidateScorer _candidateScorer;
    private readonly IPluginSettingsAccessor _settingsAccessor;
    private readonly ILogger<SmartTmdbMovieProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartTmdbMovieProvider"/> class.
    /// </summary>
    /// <param name="tmdbClient">TMDB client.</param>
    /// <param name="localMovieResolver">Local movie resolver.</param>
    /// <param name="candidateScorer">Candidate scorer.</param>
    /// <param name="settingsAccessor">Settings accessor.</param>
    /// <param name="logger">Logger.</param>
    public SmartTmdbMovieProvider(
        ITmdbClient tmdbClient,
        ILocalMovieResolver localMovieResolver,
        ICandidateScorer candidateScorer,
        IPluginSettingsAccessor settingsAccessor,
        ILogger<SmartTmdbMovieProvider> logger)
    {
        _tmdbClient = tmdbClient;
        _localMovieResolver = localMovieResolver;
        _candidateScorer = candidateScorer;
        _settingsAccessor = settingsAccessor;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string Name => ProviderName;

    /// <inheritdoc/>
    public MetadataPluginType Type => MetadataPluginType.SimilarityProvider;

    /// <inheritdoc/>
    public TimeSpan? CacheDuration => null;

    /// <inheritdoc/>
    public async IAsyncEnumerable<SimilarItemReference> GetSimilarItemsAsync(Movie item, SimilarItemsQuery query, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);

        PluginConfiguration config = _settingsAccessor.GetConfiguration();

        if (string.IsNullOrWhiteSpace(config.ApiReadAccessToken))
        {
            _logger.LogWarning("TMDB token not configured; skipping similar items for {Item}", item.Name);
            yield break;
        }

        if (!item.TryGetProviderId(MetadataProvider.Tmdb, out string? tmdbIdString) || !int.TryParse(tmdbIdString, out int tmdbId))
        {
            _logger.LogDebug("Item {Item} has no TMDB ID; skipping", item.Name);
            yield break;
        }

        SettingsSnapshot settings = new SettingsSnapshot(config);

        List<SimilarItemReference>? results = null;

        try
        {
            TmdbMovieDetailsDto sourceDetails = await _tmdbClient.GetMovieDetailsAsync(tmdbId, settings.ResponseLanguage, cancellationToken).ConfigureAwait(false);
            List<TmdbRecommendationPageDto> recommendationPages = await FetchRecommendationPagesAsync(tmdbId, settings, cancellationToken).ConfigureAwait(false);
            List<TmdbSimilarPageDto> similarPages = await FetchSimilarPagesAsync(tmdbId, settings, cancellationToken).ConfigureAwait(false);

            List<RecommendationCandidate> candidates = (List<RecommendationCandidate>)CandidateAggregator.Merge(
                sourceDetails,
                recommendationPages,
                similarPages,
                settings);

            if (candidates.Count == 0)
            {
                yield break;
            }

            IReadOnlySet<int> sourceGenreIds = sourceDetails.Genres != null
                ? new HashSet<int>(sourceDetails.Genres.Select(g => g.Id))
                : new HashSet<int>();

            DateOnly? sourceReleaseDate = ParseDateOnly(sourceDetails.ReleaseDate);
            string? sourceOriginalLanguage = sourceDetails.OriginalLanguage;

            IReadOnlyList<ScoreResult> scores = await _candidateScorer.ScoreAllAsync(
                candidates,
                sourceGenreIds,
                sourceReleaseDate,
                sourceOriginalLanguage,
                settings,
                cancellationToken).ConfigureAwait(false);

            var scored = candidates.Zip(scores, (c, s) => new { Candidate = c, Score = s })
                .Where(x => !x.Score.IsFiltered)
                .OrderByDescending(x => x.Score.Score)
                .ThenBy(x => x.Candidate.RecommendationRank ?? int.MaxValue)
                .ThenBy(x => x.Candidate.SimilarRank ?? int.MaxValue)
                .ThenBy(x => x.Candidate.TmdbId)
                .Take(MaxCandidatePool)
                .ToList();

            if (scored.Count == 0)
            {
                yield break;
            }

            List<int> candidateTmdbIds = scored.Select(x => x.Candidate.TmdbId).ToList();
            IReadOnlyDictionary<int, LocalMovieResolution> localResolutions = await _localMovieResolver.ResolveAsync(candidateTmdbIds, GetUserId(query), query.ExcludeItemIds, cancellationToken).ConfigureAwait(false);

            int resultLimit = query.Limit ?? DefaultResultLimit;

            results = new List<SimilarItemReference>();
            foreach (var entry in scored)
            {
                if (results.Count >= resultLimit)
                {
                    break;
                }

                RecommendationCandidate candidate = entry.Candidate;
                ScoreResult scoreResult = entry.Score;

                if (localResolutions.TryGetValue(candidate.TmdbId, out LocalMovieResolution? resolution))
                {
                    results.Add(new SimilarItemReference
                    {
                        ProviderName = MetadataProvider.Tmdb.ToString(),
                        ProviderId = candidate.TmdbId.ToString(),
                        Score = (float?)scoreResult.Score,
                    });
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Similar items request cancelled for {Item}", item.Name);
            yield break;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting similar items for {Item}", item.Name);
            yield break;
        }

        if (results is not null)
        {
            foreach (SimilarItemReference reference in results)
            {
                yield return reference;
            }
        }
    }

    private async Task<List<TmdbRecommendationPageDto>> FetchRecommendationPagesAsync(int tmdbId, SettingsSnapshot settings, CancellationToken cancellationToken)
    {
        if (!settings.UseRecommendations || settings.RecommendationPages <= 0)
        {
            return new List<TmdbRecommendationPageDto>();
        }

        List<TmdbRecommendationPageDto> pages = new List<TmdbRecommendationPageDto>();
        for (int page = 1; page <= settings.RecommendationPages; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TmdbRecommendationPageDto pageDto = await _tmdbClient.GetRecommendationsAsync(tmdbId, settings.ResponseLanguage, page, cancellationToken).ConfigureAwait(false);
            pages.Add(pageDto);
        }

        return pages;
    }

    private async Task<List<TmdbSimilarPageDto>> FetchSimilarPagesAsync(int tmdbId, SettingsSnapshot settings, CancellationToken cancellationToken)
    {
        if (!settings.UseSimilar || settings.SimilarPages <= 0)
        {
            return new List<TmdbSimilarPageDto>();
        }

        List<TmdbSimilarPageDto> pages = new List<TmdbSimilarPageDto>();
        for (int page = 1; page <= settings.SimilarPages; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TmdbSimilarPageDto pageDto = await _tmdbClient.GetSimilarAsync(tmdbId, settings.ResponseLanguage, page, cancellationToken).ConfigureAwait(false);
            pages.Add(pageDto);
        }

        return pages;
    }

    private static DateOnly? ParseDateOnly(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
        {
            return null;
        }

        if (DateOnly.TryParse(dateString, out DateOnly date))
        {
            return date;
        }

        return null;
    }

    private Guid? GetUserId(SimilarItemsQuery query)
    {
        if (query.User is null)
        {
            return null;
        }

        return query.User.Id;
    }
}
