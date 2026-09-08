using System;
using Jellyfin.Plugin.SmartTmdb.Configuration;
using Xunit;

namespace Jellyfin.Plugin.SmartTmdb.Tests;

public class SettingsSnapshotTests
{
    [Fact]
    public void Defaults_AreValid()
    {
        SettingsSnapshot snapshot = new SettingsSnapshot(new PluginConfiguration());

        Assert.Equal(RecommendationPreset.Balanced, snapshot.Preset);
        Assert.Equal(WatchedMode.PreferUnwatched, snapshot.WatchedMode);
        Assert.Equal(FranchiseMode.PreferNext, snapshot.FranchiseMode);
        Assert.Equal(EraMode.Soft, snapshot.EraMode);
        Assert.Equal(LanguageMode.PreferSource, snapshot.LanguageMode);
        Assert.Equal("auto", snapshot.ResponseLanguage);
        Assert.False(snapshot.IncludeAdult);
        Assert.True(snapshot.UseRecommendations);
        Assert.True(snapshot.UseSimilar);
        Assert.Equal(3, snapshot.RecommendationPages);
        Assert.Equal(1, snapshot.SimilarPages);
        Assert.Equal(5.5, snapshot.MinimumVoteAverage);
        Assert.Equal(100, snapshot.MinimumVoteCount);
        Assert.Equal(0, snapshot.PopularityBias);
        Assert.Equal(15, snapshot.EraHalfLifeYears);
        Assert.Equal(24, snapshot.RawCacheHours);
        Assert.Equal(10, snapshot.RequestTimeoutSeconds);
    }

    [Fact]
    public void Preset_Mainstream_ExpandsWeights()
    {
        SettingsSnapshot snapshot = new SettingsSnapshot(new PluginConfiguration
        {
            Preset = RecommendationPreset.Mainstream,
        });

        Assert.Equal(0.60, snapshot.CustomWeights.RecommendationRank);
        Assert.Equal(0.10, snapshot.CustomWeights.SimilarRank);
        Assert.Equal(0.05, snapshot.CustomWeights.Genre);
        Assert.Equal(0.05, snapshot.CustomWeights.Era);
        Assert.Equal(0.10, snapshot.CustomWeights.Quality);
        Assert.Equal(0.10, snapshot.CustomWeights.Popularity);
    }

    [Fact]
    public void Preset_Explorer_ExpandsWeights()
    {
        SettingsSnapshot snapshot = new SettingsSnapshot(new PluginConfiguration
        {
            Preset = RecommendationPreset.Explorer,
        });

        Assert.Equal(0.40, snapshot.CustomWeights.RecommendationRank);
        Assert.Equal(0.25, snapshot.CustomWeights.SimilarRank);
        Assert.Equal(0.15, snapshot.CustomWeights.Genre);
        Assert.Equal(0.05, snapshot.CustomWeights.Era);
        Assert.Equal(0.10, snapshot.CustomWeights.Quality);
        Assert.Equal(0.05, snapshot.CustomWeights.Popularity);
    }

    [Fact]
    public void Preset_Balanced_ExpandsWeights()
    {
        SettingsSnapshot snapshot = new SettingsSnapshot(new PluginConfiguration
        {
            Preset = RecommendationPreset.Balanced,
        });

        Assert.Equal(0.55, snapshot.CustomWeights.RecommendationRank);
        Assert.Equal(0.15, snapshot.CustomWeights.SimilarRank);
        Assert.Equal(0.10, snapshot.CustomWeights.Genre);
        Assert.Equal(0.05, snapshot.CustomWeights.Era);
        Assert.Equal(0.10, snapshot.CustomWeights.Quality);
        Assert.Equal(0.05, snapshot.CustomWeights.Popularity);
    }

    [Fact]
    public void Custom_NaNWeight_ClampedToZero()
    {
        SettingsSnapshot snapshot = new SettingsSnapshot(new PluginConfiguration
        {
            Preset = RecommendationPreset.Custom,
            CustomWeights = new ScoringWeights(double.NaN, 1.0, 0, 0, 0, 0),
        });

        Assert.Equal(0.0, snapshot.CustomWeights.RecommendationRank);
    }

    [Fact]
    public void Custom_NegativeWeight_ClampedToZero()
    {
        SettingsSnapshot snapshot = new SettingsSnapshot(new PluginConfiguration
        {
            Preset = RecommendationPreset.Custom,
            CustomWeights = new ScoringWeights(-1.0, 1.0, 0, 0, 0, 0),
        });

        Assert.Equal(0.0, snapshot.CustomWeights.RecommendationRank);
    }

    [Fact]
    public void Custom_InfinityWeight_ClampedToZero()
    {
        SettingsSnapshot snapshot = new SettingsSnapshot(new PluginConfiguration
        {
            Preset = RecommendationPreset.Custom,
            CustomWeights = new ScoringWeights(double.PositiveInfinity, 1.0, 0, 0, 0, 0),
        });

        Assert.Equal(0.0, snapshot.CustomWeights.RecommendationRank);
    }

    [Fact]
    public void RecommendationPages_ClampedToRange()
    {
        SettingsSnapshot low = new SettingsSnapshot(new PluginConfiguration { RecommendationPages = 0 });
        SettingsSnapshot high = new SettingsSnapshot(new PluginConfiguration { RecommendationPages = 10 });

        Assert.Equal(1, low.RecommendationPages);
        Assert.Equal(5, high.RecommendationPages);
    }

    [Fact]
    public void SimilarPages_ClampedToRange()
    {
        SettingsSnapshot low = new SettingsSnapshot(new PluginConfiguration { SimilarPages = 0 });
        SettingsSnapshot high = new SettingsSnapshot(new PluginConfiguration { SimilarPages = 10 });

        Assert.Equal(1, low.SimilarPages);
        Assert.Equal(5, high.SimilarPages);
    }

    [Fact]
    public void RawCacheHours_ClampedToRange()
    {
        SettingsSnapshot low = new SettingsSnapshot(new PluginConfiguration { RawCacheHours = 0 });
        SettingsSnapshot high = new SettingsSnapshot(new PluginConfiguration { RawCacheHours = 200 });

        Assert.Equal(1, low.RawCacheHours);
        Assert.Equal(168, high.RawCacheHours);
    }

    [Fact]
    public void RequestTimeoutSeconds_ClampedToRange()
    {
        SettingsSnapshot low = new SettingsSnapshot(new PluginConfiguration { RequestTimeoutSeconds = 0 });
        SettingsSnapshot high = new SettingsSnapshot(new PluginConfiguration { RequestTimeoutSeconds = 100 });

        Assert.Equal(1, low.RequestTimeoutSeconds);
        Assert.Equal(60, high.RequestTimeoutSeconds);
    }

    [Fact]
    public void ResponseLanguage_Blank_DefaultsToAuto()
    {
        SettingsSnapshot snapshot = new SettingsSnapshot(new PluginConfiguration { ResponseLanguage = "   " });

        Assert.Equal("auto", snapshot.ResponseLanguage);
    }

    [Fact]
    public void EraHalfLifeYears_ClampedToRange()
    {
        SettingsSnapshot low = new SettingsSnapshot(new PluginConfiguration { EraHalfLifeYears = 0 });
        SettingsSnapshot high = new SettingsSnapshot(new PluginConfiguration { EraHalfLifeYears = 200 });

        Assert.Equal(1, low.EraHalfLifeYears);
        Assert.Equal(100, high.EraHalfLifeYears);
    }

    [Fact]
    public void PopularityBias_ClampedToRange()
    {
        SettingsSnapshot low = new SettingsSnapshot(new PluginConfiguration { PopularityBias = -5 });
        SettingsSnapshot high = new SettingsSnapshot(new PluginConfiguration { PopularityBias = 5 });

        Assert.Equal(-2, low.PopularityBias);
        Assert.Equal(2, high.PopularityBias);
    }

    [Fact]
    public void MinimumVoteCount_Negative_ClampedToZero()
    {
        SettingsSnapshot snapshot = new SettingsSnapshot(new PluginConfiguration { MinimumVoteCount = -10 });

        Assert.Equal(0, snapshot.MinimumVoteCount);
    }
}
