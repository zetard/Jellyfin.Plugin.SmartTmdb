using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SmartTmdb.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        ApiReadAccessToken = string.Empty;
        Preset = RecommendationPreset.Balanced;
        WatchedMode = WatchedMode.PreferUnwatched;
        FranchiseMode = FranchiseMode.PreferNext;
        EraMode = EraMode.Soft;
        LanguageMode = LanguageMode.PreferSource;
        ResponseLanguage = "auto";
        IncludeAdult = false;
        UseRecommendations = true;
        UseSimilar = true;
        RecommendationPages = 3;
        SimilarPages = 1;
        MinimumVoteAverage = 5.5;
        MinimumVoteCount = 100;
        PopularityBias = 0;
        EraHalfLifeYears = 15;
        RawCacheHours = 24;
        RequestTimeoutSeconds = 10;
        CustomWeights = ScoringWeights.Balanced;
    }

    /// <summary>
    /// Gets or sets the TMDB API Read Access Token.
    /// </summary>
    public string ApiReadAccessToken { get; set; }

    /// <summary>
    /// Gets or sets the recommendation preset.
    /// </summary>
    public RecommendationPreset Preset { get; set; }

    /// <summary>
    /// Gets or sets the watched mode.
    /// </summary>
    public WatchedMode WatchedMode { get; set; }

    /// <summary>
    /// Gets or sets the franchise mode.
    /// </summary>
    public FranchiseMode FranchiseMode { get; set; }

    /// <summary>
    /// Gets or sets the era mode.
    /// </summary>
    public EraMode EraMode { get; set; }

    /// <summary>
    /// Gets or sets the language mode.
    /// </summary>
    public LanguageMode LanguageMode { get; set; }

    /// <summary>
    /// Gets or sets the response language.
    /// </summary>
    public string ResponseLanguage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include adult content.
    /// </summary>
    public bool IncludeAdult { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use TMDB recommendations.
    /// </summary>
    public bool UseRecommendations { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use TMDB similar.
    /// </summary>
    public bool UseSimilar { get; set; }

    /// <summary>
    /// Gets or sets the number of recommendation pages to fetch.
    /// </summary>
    public int RecommendationPages { get; set; }

    /// <summary>
    /// Gets or sets the number of similar pages to fetch.
    /// </summary>
    public int SimilarPages { get; set; }

    /// <summary>
    /// Gets or sets the minimum vote average.
    /// </summary>
    public double MinimumVoteAverage { get; set; }

    /// <summary>
    /// Gets or sets the minimum vote count.
    /// </summary>
    public int MinimumVoteCount { get; set; }

    /// <summary>
    /// Gets or sets the popularity bias.
    /// </summary>
    public int PopularityBias { get; set; }

    /// <summary>
    /// Gets or sets the era half-life in years.
    /// </summary>
    public int EraHalfLifeYears { get; set; }

    /// <summary>
    /// Gets or sets the raw cache hours.
    /// </summary>
    public int RawCacheHours { get; set; }

    /// <summary>
    /// Gets or sets the request timeout in seconds.
    /// </summary>
    public int RequestTimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets the custom scoring weights.
    /// </summary>
    public ScoringWeights CustomWeights { get; set; }
}
