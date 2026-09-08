namespace Jellyfin.Plugin.SmartTmdb.Tmdb;

/// <summary>
/// TMDB movie details DTO.
/// </summary>
public sealed class TmdbMovieDetailsDto
{
    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the original language.
    /// </summary>
    public string? OriginalLanguage { get; set; }

    /// <summary>
    /// Gets or sets the release date.
    /// </summary>
    public string? ReleaseDate { get; set; }

    /// <summary>
    /// Gets or sets the genres.
    /// </summary>
    public List<TmdbGenreDto>? Genres { get; set; }

    /// <summary>
    /// Gets or sets the belongs to collection ID.
    /// </summary>
    public int? BelongsToCollectionId { get; set; }

    /// <summary>
    /// Gets or sets the vote average.
    /// </summary>
    public double VoteAverage { get; set; }

    /// <summary>
    /// Gets or sets the vote count.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// Gets or sets the popularity.
    /// </summary>
    public double Popularity { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is adult.
    /// </summary>
    public bool Adult { get; set; }

    /// <summary>
    /// Gets or sets the recommendations.
    /// </summary>
    public TmdbMoviePageDto? Recommendations { get; set; }

    /// <summary>
    /// Gets or sets the similar movies.
    /// </summary>
    public TmdbMoviePageDto? Similar { get; set; }
}
