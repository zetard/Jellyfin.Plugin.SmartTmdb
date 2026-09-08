using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// Recommendation candidate record.
/// </summary>
/// <param name="TmdbId">TMDB ID.</param>
/// <param name="RecommendationRank">Recommendation rank.</param>
/// <param name="SimilarRank">Similar rank.</param>
/// <param name="GenreIds">Genre IDs.</param>
/// <param name="OriginalLanguage">Original language.</param>
/// <param name="ReleaseDate">Release date.</param>
/// <param name="VoteAverage">Vote average.</param>
/// <param name="VoteCount">Vote count.</param>
/// <param name="Popularity">Popularity.</param>
/// <param name="Adult">Adult flag.</param>
/// <param name="IsInSourceCollection">In source collection.</param>
/// <param name="IsLikelyNextCollectionPart">Likely next collection part.</param>
/// <param name="LocalItem">Local item.</param>
/// <param name="UserData">User data.</param>
public sealed record RecommendationCandidate(
    int TmdbId,
    int? RecommendationRank,
    int? SimilarRank,
    IReadOnlySet<int> GenreIds,
    string? OriginalLanguage,
    DateOnly? ReleaseDate,
    double VoteAverage,
    int VoteCount,
    double Popularity,
    bool Adult,
    bool IsInSourceCollection,
    bool IsLikelyNextCollectionPart,
    BaseItem? LocalItem,
    UserItemData? UserData);
