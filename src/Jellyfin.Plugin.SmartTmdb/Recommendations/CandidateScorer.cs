using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.SmartTmdb.Configuration;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// Pure deterministic candidate scorer. No HTTP, Jellyfin services, or wall-clock access.
/// </summary>
public sealed class CandidateScorer : ICandidateScorer
{
    private const double MaxScore = 0.95;
    private const double BayesianPriorMean = 6.0;
    private const double BayesianConfidence = 250.0;
    private const double RankDecayScale = 24.0;
    private const double RecommendationWeight = 0.55;
    private const double SimilarWeight = 0.15;
    private const double GenreWeight = 0.10;
    private const double EraWeight = 0.05;
    private const double QualityWeight = 0.10;
    private const double PopularityWeight = 0.05;
    private const double UnwatchedBonus = 0.03;
    private const double PlayedPenalty = 0.20;
    private const double LanguageMatchBonus = 0.03;
    private const double LanguageMismatchPenalty = 0.03;
    private const double LikelyNextBonus = 0.20;
    private const double SameCollectionBonus = 0.05;
    private const double AvoidSameCollectionPenalty = 0.25;

    /// <inheritdoc/>
    public Task<IReadOnlyList<ScoreResult>> ScoreAllAsync(
        IReadOnlyList<RecommendationCandidate> candidates,
        IReadOnlySet<int> sourceGenreIds,
        DateOnly? sourceReleaseDate,
        string? sourceOriginalLanguage,
        SettingsSnapshot settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(sourceGenreIds);
        ArgumentNullException.ThrowIfNull(settings);

        if (candidates.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<ScoreResult>>(Array.Empty<ScoreResult>());
        }

        double[] popularityPercentiles = ComputePopularityPercentiles(candidates);

        List<ScoreResult> results = new List<ScoreResult>(candidates.Count);
        foreach ((RecommendationCandidate candidate, double percentile) in candidates.Zip(popularityPercentiles, (c, p) => (c, p)))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            results.Add(ScoreSingle(candidate, sourceGenreIds, sourceReleaseDate, sourceOriginalLanguage, settings, percentile));
        }

        return Task.FromResult<IReadOnlyList<ScoreResult>>(results);
    }

    private static double[] ComputePopularityPercentiles(IReadOnlyList<RecommendationCandidate> candidates)
    {
        int count = candidates.Count;
        double[] percentiles = new double[count];
        if (count == 0)
        {
            return percentiles;
        }

        List<(int Index, double Value)> values = new List<(int, double)>(count);
        for (int i = 0; i < count; i++)
        {
            double logPopularity = Math.Log(1.0 + Math.Max(0.0, candidates[i].Popularity));
            values.Add((i, logPopularity));
        }

        values.Sort((a, b) => a.Value.CompareTo(b.Value));

        double minLog = values[0].Value;
        double maxLog = values[count - 1].Value;
        double range = maxLog - minLog;

        foreach ((int index, double value) in values)
        {
            double normalized = range > 0.0 ? (value - minLog) / range : 0.5;
            percentiles[index] = normalized;
        }

        return percentiles;
    }

    private static ScoreResult ScoreSingle(
        RecommendationCandidate candidate,
        IReadOnlySet<int> sourceGenreIds,
        DateOnly? sourceReleaseDate,
        string? sourceOriginalLanguage,
        SettingsSnapshot settings,
        double popularityPercentile)
    {
        bool isFiltered = false;
        List<string> reasons = new List<string>();

        if (settings.WatchedMode == WatchedMode.UnwatchedOnly && candidate.UserData is { Played: true })
        {
            return new ScoreResult(0.0f, true, reasons);
        }

        if (settings.LanguageMode == LanguageMode.OnlySource && !string.IsNullOrEmpty(sourceOriginalLanguage) && !string.IsNullOrEmpty(candidate.OriginalLanguage))
        {
            if (!string.Equals(sourceOriginalLanguage, candidate.OriginalLanguage, StringComparison.OrdinalIgnoreCase))
            {
                return new ScoreResult(0.0f, true, reasons);
            }
        }

        if (settings.EraMode == EraMode.Strict10 && sourceReleaseDate.HasValue && candidate.ReleaseDate.HasValue)
        {
            int yearDiff = Math.Abs(sourceReleaseDate.Value.Year - candidate.ReleaseDate.Value.Year);
            if (yearDiff > 10)
            {
                return new ScoreResult(0.0f, true, reasons);
            }
        }

        double recRankScore = ComputeRankScore(candidate.RecommendationRank);
        double simRankScore = ComputeRankScore(candidate.SimilarRank);
        double genreScore = ComputeGenreScore(sourceGenreIds, candidate.GenreIds);
        double eraScore = ComputeEraScore(sourceReleaseDate, candidate.ReleaseDate, settings.EraHalfLifeYears);
        double qualityScore = ComputeQualityScore(candidate.VoteAverage, candidate.VoteCount);
        double popularityScore = ComputePopularityScore(popularityPercentile, settings.PopularityBias);

        double baseScore = settings.NormalizedWeights.RecommendationRank * recRankScore
            + settings.NormalizedWeights.SimilarRank * simRankScore
            + settings.NormalizedWeights.Genre * genreScore
            + settings.NormalizedWeights.Era * eraScore
            + settings.NormalizedWeights.Quality * qualityScore
            + settings.NormalizedWeights.Popularity * popularityScore;

        double adjustments = 0.0;

        if (candidate.UserData is { Played: true })
        {
            if (settings.WatchedMode == WatchedMode.PreferUnwatched)
            {
                adjustments -= PlayedPenalty;
                reasons.Add($"played-penalty:-{PlayedPenalty:0.00}");
            }
        }
        else if (settings.WatchedMode == WatchedMode.PreferUnwatched && candidate.UserData is { Played: false })
        {
            adjustments += UnwatchedBonus;
            reasons.Add($"unwatched-bonus:+{UnwatchedBonus:0.00}");
        }

        if (!string.IsNullOrEmpty(sourceOriginalLanguage) && !string.IsNullOrEmpty(candidate.OriginalLanguage))
        {
            if (string.Equals(sourceOriginalLanguage, candidate.OriginalLanguage, StringComparison.OrdinalIgnoreCase))
            {
                if (settings.LanguageMode == LanguageMode.PreferSource)
                {
                    adjustments += LanguageMatchBonus;
                    reasons.Add($"language-match:+{LanguageMatchBonus:0.00}");
                }
            }
            else
            {
                if (settings.LanguageMode == LanguageMode.PreferSource)
                {
                    adjustments -= LanguageMismatchPenalty;
                    reasons.Add($"language-mismatch:-{LanguageMismatchPenalty:0.00}");
                }
            }
        }

        if (settings.FranchiseMode == FranchiseMode.PreferNext)
        {
            if (candidate.IsLikelyNextCollectionPart)
            {
                adjustments += LikelyNextBonus;
                reasons.Add($"likely-next:+{LikelyNextBonus:0.00}");
            }
            else if (candidate.IsInSourceCollection)
            {
                adjustments += SameCollectionBonus;
                reasons.Add($"same-collection:+{SameCollectionBonus:0.00}");
            }
        }
        else if (settings.FranchiseMode == FranchiseMode.Avoid && candidate.IsInSourceCollection)
        {
            adjustments -= AvoidSameCollectionPenalty;
            reasons.Add($"avoid-collection:-{AvoidSameCollectionPenalty:0.00}");
        }

        if (double.IsNaN(baseScore) || double.IsInfinity(baseScore))
        {
            baseScore = 0.0;
        }

        double finalScore = baseScore + adjustments;
        finalScore = Math.Clamp(finalScore, 0.0, MaxScore);

        if (double.IsNaN(finalScore) || double.IsInfinity(finalScore))
        {
            finalScore = 0.0;
        }

        reasons.Add($"base:{baseScore:0.00}");
        reasons.Add($"final:{finalScore:0.00}");

        return new ScoreResult((float)finalScore, isFiltered, reasons);
    }

    private static double ComputeRankScore(int? rank)
    {
        if (!rank.HasValue || rank.Value <= 0)
        {
            return 0.0;
        }

        return Math.Exp(-(rank.Value - 1) / RankDecayScale);
    }

    private static double ComputeGenreScore(IReadOnlySet<int> sourceGenres, IReadOnlySet<int> candidateGenres)
    {
        if (sourceGenres.Count == 0 || candidateGenres.Count == 0)
        {
            return 0.0;
        }

        int intersection = 0;
        foreach (int genreId in sourceGenres)
        {
            if (candidateGenres.Contains(genreId))
            {
                intersection++;
            }
        }

        int union = sourceGenres.Count + candidateGenres.Count - intersection;
        if (union == 0)
        {
            return 0.0;
        }

        return (double)intersection / union;
    }

    private static double ComputeEraScore(DateOnly? sourceDate, DateOnly? candidateDate, int halfLifeYears)
    {
        if (!sourceDate.HasValue || !candidateDate.HasValue)
        {
            return 0.0;
        }

        int yearDiff = Math.Abs(sourceDate.Value.Year - candidateDate.Value.Year);
        double halfLife = Math.Max(halfLifeYears, 1);
        return Math.Exp(-Math.Log(2.0) * yearDiff / halfLife);
    }

    private static double ComputeQualityScore(double voteAverage, int voteCount)
    {
        double r = voteAverage;
        double v = Math.Max(voteCount, 0);
        double m = BayesianConfidence;
        double c = BayesianPriorMean;

        return (v / (v + m)) * (r / 10.0) + (m / (v + m)) * (c / 10.0);
    }

    private static double ComputePopularityScore(double percentile, int popularityBias)
    {
        if (popularityBias == 0)
        {
            return 0.0;
        }

        if (popularityBias > 0)
        {
            return percentile;
        }

        return 1.0 - percentile;
    }
}
