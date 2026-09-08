namespace Jellyfin.Plugin.SmartTmdb.Tmdb;

/// <summary>
/// TMDB movie result DTO.
/// </summary>
public sealed class TmdbMovieResultDto
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
    /// Gets or sets the genre IDs.
    /// </summary>
    public List<int>? GenreIds { get; set; }

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
    /// Gets or sets the backdrop path.
    /// </summary>
    public string? BackdropPath { get; set; }

    /// <summary>
    /// Gets or sets the poster path.
    /// </summary>
    public string? PosterPath { get; set; }

    /// <summary>
    /// Gets or sets the overview.
    /// </summary>
    public string? Overview { get; set; }

    /// <summary>
    /// Gets or sets the runtime in minutes.
    /// </summary>
    public int? Runtime { get; set; }
}
