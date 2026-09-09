using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.SmartTmdb;
using Jellyfin.Plugin.SmartTmdb.Configuration;
using Jellyfin.Plugin.SmartTmdb.Tmdb;
using MediaBrowser.Controller.Entities;
using Xunit;

namespace Jellyfin.Plugin.SmartTmdb.Tests;

public class CandidateScorerTests
{
    private static SettingsSnapshot CreateSettings(ScoringWeights? weights = null, RecommendationPreset preset = RecommendationPreset.Custom)
    {
        return new SettingsSnapshot(new PluginConfiguration
        {
            Preset = preset,
            CustomWeights = weights ?? ScoringWeights.Balanced,
            WatchedMode = WatchedMode.Allow,
            FranchiseMode = FranchiseMode.Neutral,
            EraMode = EraMode.Off,
            LanguageMode = LanguageMode.Any,
            PopularityBias = 0,
            MinimumVoteAverage = 0.0,
            MinimumVoteCount = 0,
            IncludeAdult = true,
        });
    }

    private static RecommendationCandidate CreateCandidate(
        int tmdbId = 1,
        int? recommendationRank = 1,
        int? similarRank = null,
        IReadOnlySet<int>? genreIds = null,
        string? originalLanguage = null,
        DateOnly? releaseDate = null,
        double voteAverage = 7.0,
        int voteCount = 1000,
        double popularity = 100.0,
        bool adult = false,
        bool isInSourceCollection = false,
        bool isLikelyNextCollectionPart = false,
        BaseItem? localItem = null,
        UserItemData? userData = null)
    {
        return new RecommendationCandidate(
            tmdbId,
            recommendationRank,
            similarRank,
            genreIds ?? new HashSet<int>(),
            originalLanguage,
            releaseDate,
            voteAverage,
            voteCount,
            popularity,
            adult,
            isInSourceCollection,
            isLikelyNextCollectionPart,
            localItem,
            userData);
    }

    [Fact]
    public void RankDecay_DecreasesWithRank()
    {
        double score1 = Math.Exp(-(1 - 1) / 24.0);
        double score25 = Math.Exp(-(25 - 1) / 24.0);
        double score100 = Math.Exp(-(100 - 1) / 24.0);

        Assert.Equal(1.0, score1);
        Assert.True(score25 > 0.0);
        Assert.True(score100 > 0.0);
        Assert.True(score1 > score25);
        Assert.True(score25 > score100);
    }

    [Fact]
    public void GenreJaccard_ReturnsCorrectOverlap()
    {
        var sourceGenres = new HashSet<int> { 1, 2, 3 };
        var candidateGenres = new HashSet<int> { 2, 3, 4 };

        double score = ComputeGenreScore(sourceGenres, candidateGenres);
        Assert.Equal(2.0 / 4.0, score);
    }

    [Fact]
    public void GenreJaccard_EmptyUnion_ReturnsZero()
    {
        var sourceGenres = new HashSet<int>();
        var candidateGenres = new HashSet<int> { 1 };

        double score = ComputeGenreScore(sourceGenres, candidateGenres);
        Assert.Equal(0.0, score);
    }

    [Fact]
    public void EraHalfLife_ExponentialDecay()
    {
        DateOnly? source = new DateOnly(2000, 1, 1);
        DateOnly? candidate = new DateOnly(2015, 1, 1);

        double score15 = ComputeEraScore(source, candidate, 15);
        double score30 = ComputeEraScore(source, new DateOnly(2030, 1, 1), 15);

        Assert.Equal(0.5, score15, 5);
        Assert.True(score30 < score15);
    }

    [Fact]
    public void BayesianQuality_PenalizesLowVotes()
    {
        double highVotes = ComputeQualityScore(9.5, 12);
        double lowVotes = ComputeQualityScore(8.0, 100000);

        Assert.True(lowVotes > highVotes);
    }

    [Fact]
    public async Task PopularityDirection_MainstreamFavorsPopular()
    {
        var candidates = new List<RecommendationCandidate>
        {
            CreateCandidate(popularity: 10.0, tmdbId: 1),
            CreateCandidate(popularity: 1000.0, tmdbId: 2),
        };

        SettingsSnapshot settings = CreateSettings(ScoringWeights.Mainstream, RecommendationPreset.Mainstream);
        settings = new SettingsSnapshot(new PluginConfiguration { Preset = RecommendationPreset.Mainstream, PopularityBias = 1 });
        var scorer = new CandidateScorer();
        var results = await scorer.ScoreAllAsync(candidates, new HashSet<int>(), null, null, settings, CancellationToken.None);

        Assert.True(results[1].Score > results[0].Score);
    }

    [Fact]
    public async Task PopularityDirection_ExplorerFavorsUnpopular()
    {
        var candidates = new List<RecommendationCandidate>
        {
            CreateCandidate(popularity: 10.0, tmdbId: 1),
            CreateCandidate(popularity: 1000.0, tmdbId: 2),
        };

        SettingsSnapshot settings = CreateSettings(ScoringWeights.Explorer, RecommendationPreset.Explorer);
        settings = new SettingsSnapshot(new PluginConfiguration { Preset = RecommendationPreset.Explorer, PopularityBias = -1 });
        var scorer = new CandidateScorer();
        var results = await scorer.ScoreAllAsync(candidates, new HashSet<int>(), null, null, settings, CancellationToken.None);

        Assert.True(results[0].Score > results[1].Score);
    }

    [Fact]
    public async Task WatchedMode_PreferUnwatched_PenalizesPlayed()
    {
        var played = CreateCandidate(userData: new UserItemData { Key = "0", Played = true }, tmdbId: 1);
        var unplayed = CreateCandidate(userData: new UserItemData { Key = "0", Played = false }, tmdbId: 2);

        SettingsSnapshot settings = CreateSettings();
        settings = new SettingsSnapshot(new PluginConfiguration { WatchedMode = WatchedMode.PreferUnwatched });
        var scorer = new CandidateScorer();
        var results = await scorer.ScoreAllAsync(new List<RecommendationCandidate> { played, unplayed }, new HashSet<int>(), null, null, settings, CancellationToken.None);

        Assert.True(results[1].Score > results[0].Score);
    }

    [Fact]
    public async Task WatchedMode_UnwatchedOnly_FiltersPlayed()
    {
        var played = CreateCandidate(userData: new UserItemData { Key = "0", Played = true }, tmdbId: 1);
        var unplayed = CreateCandidate(userData: new UserItemData { Key = "0", Played = false }, tmdbId: 2);

        SettingsSnapshot settings = CreateSettings();
        settings = new SettingsSnapshot(new PluginConfiguration { WatchedMode = WatchedMode.UnwatchedOnly });
        var scorer = new CandidateScorer();
        var results = await scorer.ScoreAllAsync(new List<RecommendationCandidate> { played, unplayed }, new HashSet<int>(), null, null, settings, CancellationToken.None);

        Assert.True(results[0].IsFiltered);
        Assert.False(results[1].IsFiltered);
    }

    [Fact]
    public async Task FranchiseMode_PreferNext_Bonuses()
    {
        var likelyNext = CreateCandidate(isInSourceCollection: true, isLikelyNextCollectionPart: true, tmdbId: 1);
        var sameCollection = CreateCandidate(isInSourceCollection: true, isLikelyNextCollectionPart: false, tmdbId: 2);
        var outside = CreateCandidate(isInSourceCollection: false, tmdbId: 3);

        SettingsSnapshot settings = CreateSettings();
        settings = new SettingsSnapshot(new PluginConfiguration { FranchiseMode = FranchiseMode.PreferNext });
        var scorer = new CandidateScorer();
        var results = await scorer.ScoreAllAsync(new List<RecommendationCandidate> { likelyNext, sameCollection, outside }, new HashSet<int>(), null, null, settings, CancellationToken.None);

        Assert.True(results[0].Score > results[1].Score);
        Assert.True(results[1].Score > results[2].Score);
    }

    [Fact]
    public async Task FranchiseMode_Avoid_PenalizesSameCollection()
    {
        var sameCollection = CreateCandidate(isInSourceCollection: true, tmdbId: 1);
        var outside = CreateCandidate(isInSourceCollection: false, tmdbId: 2);

        SettingsSnapshot settings = CreateSettings();
        settings = new SettingsSnapshot(new PluginConfiguration { FranchiseMode = FranchiseMode.Avoid });
        var scorer = new CandidateScorer();
        var results = await scorer.ScoreAllAsync(new List<RecommendationCandidate> { sameCollection, outside }, new HashSet<int>(), null, null, settings, CancellationToken.None);

        Assert.True(results[1].Score > results[0].Score);
    }

    [Fact]
    public async Task Score_ClampedTo095()
    {
        var candidate = CreateCandidate(recommendationRank: 1, similarRank: 1);
        var sourceGenres = new HashSet<int> { 1, 2, 3 };
        var candidateGenres = new HashSet<int>(sourceGenres);

        SettingsSnapshot settings = CreateSettings(weights: new ScoringWeights(1.0, 1.0, 1.0, 1.0, 1.0, 1.0));
        var scorer = new CandidateScorer();
        var results = await scorer.ScoreAllAsync(
            new List<RecommendationCandidate> { candidate with { GenreIds = candidateGenres } },
            sourceGenres,
            candidate.ReleaseDate,
            candidate.OriginalLanguage,
            settings,
            CancellationToken.None);

        Assert.True(results[0].Score <= 0.95f);
    }

    [Fact]
    public void SettingsSnapshot_RejectsZeroWeights()
    {
        Assert.Throws<InvalidOperationException>(() => new SettingsSnapshot(new PluginConfiguration
        {
            Preset = RecommendationPreset.Custom,
            CustomWeights = new ScoringWeights(0, 0, 0, 0, 0, 0),
        }));
    }

    [Fact]
    public void SettingsSnapshot_NaNWeight_ClampedToZero()
    {
        SettingsSnapshot snapshot = new SettingsSnapshot(new PluginConfiguration
        {
            Preset = RecommendationPreset.Custom,
            CustomWeights = new ScoringWeights(double.NaN, 1.0, 0, 0, 0, 0),
        });

        Assert.Equal(0.0, snapshot.CustomWeights.RecommendationRank);
    }

    [Fact]
    public void SettingsSnapshot_NegativeWeight_ClampedToZero()
    {
        SettingsSnapshot snapshot = new SettingsSnapshot(new PluginConfiguration
        {
            Preset = RecommendationPreset.Custom,
            CustomWeights = new ScoringWeights(-1.0, 1.0, 0, 0, 0, 0),
        });

        Assert.Equal(0.0, snapshot.CustomWeights.RecommendationRank);
    }

    [Fact]
    public void SettingsSnapshot_NormalizesWeights()
    {
        SettingsSnapshot snapshot = new SettingsSnapshot(new PluginConfiguration
        {
            Preset = RecommendationPreset.Custom,
            CustomWeights = new ScoringWeights(2.0, 2.0, 1.0, 0, 0, 0),
        });

        Assert.Equal(1.0, snapshot.NormalizedWeights.RecommendationRank + snapshot.NormalizedWeights.SimilarRank + snapshot.NormalizedWeights.Genre, 5);
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
        double m = 250.0;
        double c = 6.0;

        return (v / (v + m)) * (r / 10.0) + (m / (v + m)) * (c / 10.0);
    }
}
