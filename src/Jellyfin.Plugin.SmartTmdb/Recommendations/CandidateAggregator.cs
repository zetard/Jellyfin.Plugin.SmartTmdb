using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.SmartTmdb.Configuration;
using Jellyfin.Plugin.SmartTmdb.Tmdb;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// Aggregates TMDB recommendation and similar candidates into a unified pool.
/// </summary>
public static class CandidateAggregator
{
    private const int MaxTotalCandidates = 200;

    /// <summary>
    /// Merges candidates from source details, recommendations pages, and similar pages.
    /// </summary>
    /// <param name="sourceDetails">Source movie details.</param>
    /// <param name="recommendationPages">Recommendation pages.</param>
    /// <param name="similarPages">Similar pages.</param>
    /// <param name="settings">Settings snapshot.</param>
    /// <returns>Aggregated candidates.</returns>
    public static IReadOnlyList<RecommendationCandidate> Merge(
        TmdbMovieDetailsDto sourceDetails,
        IReadOnlyList<TmdbRecommendationPageDto> recommendationPages,
        IReadOnlyList<TmdbSimilarPageDto> similarPages,
        SettingsSnapshot settings)
    {
        ArgumentNullException.ThrowIfNull(sourceDetails);
        ArgumentNullException.ThrowIfNull(recommendationPages);
        ArgumentNullException.ThrowIfNull(similarPages);
        ArgumentNullException.ThrowIfNull(settings);

        Dictionary<int, RecommendationCandidate> candidates = new Dictionary<int, RecommendationCandidate>();
        int sourceTmdbId = sourceDetails.Id;

        HashSet<int> sourceGenreIds = new HashSet<int>();
        if (sourceDetails.Genres != null)
        {
            foreach (TmdbGenreDto genre in sourceDetails.Genres)
            {
                sourceGenreIds.Add(genre.Id);
            }
        }

        DateOnly? sourceReleaseDate = ParseDateOnly(sourceDetails.ReleaseDate);

        void AddCandidate(TmdbMovieResultDto result, int? recommendationRank, int? similarRank)
        {
            if (result.Id == sourceTmdbId)
            {
                return;
            }

            if (!settings.IncludeAdult && result.Adult)
            {
                return;
            }

            if (result.VoteCount < settings.MinimumVoteCount)
            {
                return;
            }

            if (result.VoteAverage < settings.MinimumVoteAverage)
            {
                return;
            }

            if (candidates.Count >= MaxTotalCandidates)
            {
                return;
            }

            HashSet<int> genreIds = new HashSet<int>();
            if (result.GenreIds != null)
            {
                foreach (int id in result.GenreIds)
                {
                    genreIds.Add(id);
                }
            }

            DateOnly? releaseDate = ParseDateOnly(result.ReleaseDate);

            if (candidates.TryGetValue(result.Id, out RecommendationCandidate? existing))
            {
                int? bestRecRank = Min(existing.RecommendationRank, recommendationRank);
                int? bestSimRank = Min(existing.SimilarRank, similarRank);

                candidates[result.Id] = existing with
                {
                    RecommendationRank = bestRecRank,
                    SimilarRank = bestSimRank,
                    GenreIds = genreIds.Count > 0 ? genreIds : existing.GenreIds,
                    OriginalLanguage = existing.OriginalLanguage ?? result.OriginalLanguage,
                    ReleaseDate = existing.ReleaseDate ?? releaseDate,
                    VoteAverage = existing.VoteCount > result.VoteCount ? existing.VoteAverage : result.VoteAverage,
                    VoteCount = Math.Max(existing.VoteCount, result.VoteCount),
                    Popularity = Math.Max(existing.Popularity, result.Popularity),
                    Adult = existing.Adult || result.Adult,
                };
            }
            else
            {
                candidates[result.Id] = new RecommendationCandidate(
                    result.Id,
                    recommendationRank,
                    similarRank,
                    genreIds,
                    result.OriginalLanguage,
                    releaseDate,
                    result.VoteAverage,
                    result.VoteCount,
                    result.Popularity,
                    result.Adult,
                    false,
                    false,
                    null,
                    null);
            }
        }

        if (settings.UseRecommendations && sourceDetails.Recommendations?.Results != null)
        {
            foreach (TmdbMovieResultDto result in sourceDetails.Recommendations.Results)
            {
                AddCandidate(result, recommendationRank: 1, similarRank: null);
            }
        }

        if (settings.UseSimilar && sourceDetails.Similar?.Results != null)
        {
            foreach (TmdbMovieResultDto result in sourceDetails.Similar.Results)
            {
                AddCandidate(result, recommendationRank: null, similarRank: 1);
            }
        }

        foreach (TmdbRecommendationPageDto page in recommendationPages)
        {
            if (page.Results == null)
            {
                continue;
            }

            foreach (TmdbMovieResultDto result in page.Results)
            {
                int? rank = result == page.Results[0] ? 1 : null;
                if (rank.HasValue && page.Page > 1)
                {
                    rank = (page.Page - 1) * 20 + 1;
                }

                AddCandidate(result, recommendationRank: rank, similarRank: null);
            }
        }

        foreach (TmdbSimilarPageDto page in similarPages)
        {
            if (page.Results == null)
            {
                continue;
            }

            foreach (TmdbMovieResultDto result in page.Results)
            {
                int? rank = result == page.Results[0] ? 1 : null;
                if (rank.HasValue && page.Page > 1)
                {
                    rank = (page.Page - 1) * 20 + 1;
                }

                AddCandidate(result, recommendationRank: null, similarRank: rank);
            }
        }

        return candidates.Values.ToList();
    }

    private static int? Min(int? left, int? right)
    {
        if (!left.HasValue)
        {
            return right;
        }

        if (!right.HasValue)
        {
            return left;
        }

        return left.Value < right.Value ? left : right;
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
}
