using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SmartTmdb.Tmdb;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// TMDB client.
/// </summary>
public interface ITmdbClient
{
    /// <summary>
    /// Gets movie details with appended recommendations and similar.
    /// </summary>
    /// <param name="tmdbId">TMDB ID.</param>
    /// <param name="language">Language.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Movie details DTO.</returns>
    Task<TmdbMovieDetailsDto> GetMovieDetailsAsync(int tmdbId, string? language, CancellationToken cancellationToken);

    /// <summary>
    /// Gets recommendations for a movie.
    /// </summary>
    /// <param name="tmdbId">TMDB ID.</param>
    /// <param name="language">Language.</param>
    /// <param name="page">Page number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recommendation page.</returns>
    Task<TmdbRecommendationPageDto> GetRecommendationsAsync(int tmdbId, string? language, int page, CancellationToken cancellationToken);

    /// <summary>
    /// Gets similar movies.
    /// </summary>
    /// <param name="tmdbId">TMDB ID.</param>
    /// <param name="language">Language.</param>
    /// <param name="page">Page number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Similar movies page.</returns>
    Task<TmdbSimilarPageDto> GetSimilarAsync(int tmdbId, string? language, int page, CancellationToken cancellationToken);

    /// <summary>
    /// Gets collection details.
    /// </summary>
    /// <param name="collectionId">Collection ID.</param>
    /// <param name="language">Language.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection details.</returns>
    Task<TmdbCollectionDto> GetCollectionAsync(int collectionId, string? language, CancellationToken cancellationToken);
}
