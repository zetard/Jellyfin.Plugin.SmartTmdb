using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.SmartTmdb.Configuration;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// Validated immutable settings snapshot for scoring and candidate generation.
/// </summary>
public sealed class SettingsSnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsSnapshot"/> class.
    /// </summary>
    /// <param name="configuration">Plugin configuration.</param>
    public SettingsSnapshot(PluginConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        ApiReadAccessToken = configuration.ApiReadAccessToken ?? string.Empty;
        Preset = configuration.Preset;
        WatchedMode = configuration.WatchedMode;
        FranchiseMode = configuration.FranchiseMode;
        EraMode = configuration.EraMode;
        LanguageMode = configuration.LanguageMode;
        ResponseLanguage = string.IsNullOrWhiteSpace(configuration.ResponseLanguage) ? "auto" : configuration.ResponseLanguage.Trim();
        IncludeAdult = configuration.IncludeAdult;
        UseRecommendations = configuration.UseRecommendations;
        UseSimilar = configuration.UseSimilar;
        RecommendationPages = Math.Clamp(configuration.RecommendationPages, 1, 5);
        SimilarPages = Math.Clamp(configuration.SimilarPages, 1, 5);
        MinimumVoteAverage = configuration.MinimumVoteAverage;
        MinimumVoteCount = Math.Max(configuration.MinimumVoteCount, 0);
        PopularityBias = Math.Clamp(configuration.PopularityBias, -2, 2);
        EraHalfLifeYears = Math.Clamp(configuration.EraHalfLifeYears, 1, 100);
        RawCacheHours = Math.Clamp(configuration.RawCacheHours, 1, 168);
        RequestTimeoutSeconds = Math.Clamp(configuration.RequestTimeoutSeconds, 1, 60);

        ScoringWeights weights = configuration.Preset switch
        {
            RecommendationPreset.Mainstream => ScoringWeights.Mainstream,
            RecommendationPreset.Explorer => ScoringWeights.Explorer,
            RecommendationPreset.Custom => configuration.CustomWeights,
            _ => ScoringWeights.Balanced,
        };

        weights = new ScoringWeights(
            SanitizeWeight(weights.RecommendationRank),
            SanitizeWeight(weights.SimilarRank),
            SanitizeWeight(weights.Genre),
            SanitizeWeight(weights.Era),
            SanitizeWeight(weights.Quality),
            SanitizeWeight(weights.Popularity));

        ScoringWeights normalizedWeights = weights;
        if (PopularityBias == 0)
        {
            normalizedWeights = new ScoringWeights(
                weights.RecommendationRank,
                weights.SimilarRank,
                weights.Genre,
                weights.Era,
                weights.Quality,
                0);
        }

        double totalWeight = normalizedWeights.RecommendationRank + normalizedWeights.SimilarRank + normalizedWeights.Genre + normalizedWeights.Era + normalizedWeights.Quality + normalizedWeights.Popularity;
        if (totalWeight <= 0)
        {
            throw new InvalidOperationException("Scoring weights sum to zero or negative; at least one weight must be positive.");
        }

        CustomWeights = weights;
        NormalizedWeights = new ScoringWeights(
            normalizedWeights.RecommendationRank / totalWeight,
            normalizedWeights.SimilarRank / totalWeight,
            normalizedWeights.Genre / totalWeight,
            normalizedWeights.Era / totalWeight,
            normalizedWeights.Quality / totalWeight,
            normalizedWeights.Popularity / totalWeight);
    }

    private static double SanitizeWeight(double value)
    {
        return double.IsNaN(value) || double.IsInfinity(value) || value < 0 ? 0 : value;
    }

    /// <summary>
    /// Gets the API read access token.
    /// </summary>
    public string ApiReadAccessToken { get; }

    /// <summary>
    /// Gets the recommendation preset.
    /// </summary>
    public RecommendationPreset Preset { get; }

    /// <summary>
    /// Gets the watched mode.
    /// </summary>
    public WatchedMode WatchedMode { get; }

    /// <summary>
    /// Gets the franchise mode.
    /// </summary>
    public FranchiseMode FranchiseMode { get; }

    /// <summary>
    /// Gets the era mode.
    /// </summary>
    public EraMode EraMode { get; }

    /// <summary>
    /// Gets the language mode.
    /// </summary>
    public LanguageMode LanguageMode { get; }

    /// <summary>
    /// Gets the response language.
    /// </summary>
    public string ResponseLanguage { get; }

    /// <summary>
    /// Gets a value indicating whether to include adult content.
    /// </summary>
    public bool IncludeAdult { get; }

    /// <summary>
    /// Gets a value indicating whether to use recommendations.
    /// </summary>
    public bool UseRecommendations { get; }

    /// <summary>
    /// Gets a value indicating whether to use similar.
    /// </summary>
    public bool UseSimilar { get; }

    /// <summary>
    /// Gets the number of recommendation pages.
    /// </summary>
    public int RecommendationPages { get; }

    /// <summary>
    /// Gets the number of similar pages.
    /// </summary>
    public int SimilarPages { get; }

    /// <summary>
    /// Gets the minimum vote average.
    /// </summary>
    public double MinimumVoteAverage { get; }

    /// <summary>
    /// Gets the minimum vote count.
    /// </summary>
    public int MinimumVoteCount { get; }

    /// <summary>
    /// Gets the popularity bias.
    /// </summary>
    public int PopularityBias { get; }

    /// <summary>
    /// Gets the era half-life in years.
    /// </summary>
    public int EraHalfLifeYears { get; }

    /// <summary>
    /// Gets the raw cache hours.
    /// </summary>
    public int RawCacheHours { get; }

    /// <summary>
    /// Gets the request timeout in seconds.
    /// </summary>
    public int RequestTimeoutSeconds { get; }

    /// <summary>
    /// Gets the custom scoring weights.
    /// </summary>
    public ScoringWeights CustomWeights { get; }

    /// <summary>
    /// Gets the normalized scoring weights that sum to 1.0.
    /// </summary>
    public ScoringWeights NormalizedWeights { get; }
}
