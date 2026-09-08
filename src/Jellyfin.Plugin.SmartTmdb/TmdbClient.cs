using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SmartTmdb.Tmdb;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// TMDB client skeleton.
/// </summary>
public sealed class TmdbClient : ITmdbClient
{
    /// <inheritdoc/>
    public Task<TmdbMovieDetailsDto> GetMovieDetailsAsync(int tmdbId, string? language, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<TmdbRecommendationPageDto> GetRecommendationsAsync(int tmdbId, string? language, int page, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<TmdbSimilarPageDto> GetSimilarAsync(int tmdbId, string? language, int page, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<TmdbCollectionDto> GetCollectionAsync(int collectionId, string? language, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
