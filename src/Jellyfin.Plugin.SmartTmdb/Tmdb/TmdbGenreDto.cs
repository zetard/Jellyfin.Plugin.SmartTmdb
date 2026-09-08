namespace Jellyfin.Plugin.SmartTmdb.Tmdb;

/// <summary>
/// TMDB genre DTO.
/// </summary>
public sealed class TmdbGenreDto
{
    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string? Name { get; set; }
}
