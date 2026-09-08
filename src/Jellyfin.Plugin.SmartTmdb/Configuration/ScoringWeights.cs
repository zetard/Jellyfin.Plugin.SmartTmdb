namespace Jellyfin.Plugin.SmartTmdb.Configuration;

/// <summary>
/// Scoring weights.
/// </summary>
public sealed record ScoringWeights(
    double RecommendationRank = 0.55,
    double SimilarRank = 0.15,
    double Genre = 0.10,
    double Era = 0.05,
    double Quality = 0.10,
    double Popularity = 0.05)
{
    /// <summary>
    /// Gets the balanced weights.
    /// </summary>
    public static ScoringWeights Balanced => new();

    /// <summary>
    /// Gets the mainstream weights.
    /// </summary>
    public static ScoringWeights Mainstream => new(0.60, 0.10, 0.05, 0.05, 0.10, 0.10);

    /// <summary>
    /// Gets the explorer weights.
    /// </summary>
    public static ScoringWeights Explorer => new(0.40, 0.25, 0.15, 0.05, 0.10, 0.05);
}
