namespace Jellyfin.Plugin.SmartTmdb.Configuration;

/// <summary>
/// Scoring weights used by the recommendation engine.
/// </summary>
public sealed class ScoringWeights
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScoringWeights"/> class.
    /// </summary>
    public ScoringWeights()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScoringWeights"/> class.
    /// </summary>
    /// <param name="recommendationRank">Recommendation rank weight.</param>
    /// <param name="similarRank">Similar rank weight.</param>
    /// <param name="genre">Genre weight.</param>
    /// <param name="era">Era weight.</param>
    /// <param name="quality">Quality weight.</param>
    /// <param name="popularity">Popularity weight.</param>
    public ScoringWeights(double recommendationRank, double similarRank, double genre, double era, double quality, double popularity)
    {
        RecommendationRank = recommendationRank;
        SimilarRank = similarRank;
        Genre = genre;
        Era = era;
        Quality = quality;
        Popularity = popularity;
    }

    /// <summary>
    /// Gets or sets the recommendation rank weight.
    /// </summary>
    public double RecommendationRank { get; set; }

    /// <summary>
    /// Gets or sets the similar rank weight.
    /// </summary>
    public double SimilarRank { get; set; }

    /// <summary>
    /// Gets or sets the genre weight.
    /// </summary>
    public double Genre { get; set; }

    /// <summary>
    /// Gets or sets the era weight.
    /// </summary>
    public double Era { get; set; }

    /// <summary>
    /// Gets or sets the quality weight.
    /// </summary>
    public double Quality { get; set; }

    /// <summary>
    /// Gets or sets the popularity weight.
    /// </summary>
    public double Popularity { get; set; }

    /// <summary>
    /// Gets the balanced preset.
    /// </summary>
    public static ScoringWeights Balanced { get; } = new(0.55, 0.15, 0.10, 0.05, 0.10, 0.05);

    /// <summary>
    /// Gets the mainstream preset.
    /// </summary>
    public static ScoringWeights Mainstream { get; } = new(0.60, 0.10, 0.05, 0.05, 0.10, 0.10);

    /// <summary>
    /// Gets the explorer preset.
    /// </summary>
    public static ScoringWeights Explorer { get; } = new(0.40, 0.25, 0.15, 0.05, 0.10, 0.05);

    /// <summary>
    /// Returns a copy of this instance with the specified popularity value.
    /// </summary>
    /// <param name="popularity">The popularity value.</param>
    /// <returns>A new <see cref="ScoringWeights"/> instance.</returns>
    public ScoringWeights WithPopularity(double popularity)
    {
        return new ScoringWeights(
            RecommendationRank,
            SimilarRank,
            Genre,
            Era,
            Quality,
            popularity);
    }
}