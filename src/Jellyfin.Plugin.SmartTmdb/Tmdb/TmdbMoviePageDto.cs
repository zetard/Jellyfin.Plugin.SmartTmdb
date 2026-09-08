namespace Jellyfin.Plugin.SmartTmdb.Tmdb;

/// <summary>
/// TMDB movie page DTO.
/// </summary>
public class TmdbMoviePageDto
{
    /// <summary>
    /// Gets or sets the page.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the total pages.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets or sets the results.
    /// </summary>
    public List<TmdbMovieResultDto>? Results { get; set; }
}
