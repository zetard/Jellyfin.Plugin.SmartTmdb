namespace Jellyfin.Plugin.SmartTmdb.Tmdb;

/// <summary>
/// TMDB collection part DTO.
/// </summary>
public sealed class TmdbCollectionPartDto
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
    /// Gets or sets the release date.
    /// </summary>
    public string? ReleaseDate { get; set; }
}
