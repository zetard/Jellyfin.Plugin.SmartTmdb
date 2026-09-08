namespace Jellyfin.Plugin.SmartTmdb.Tmdb;

/// <summary>
/// TMDB collection DTO.
/// </summary>
public sealed class TmdbCollectionDto
{
    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the parts.
    /// </summary>
    public List<TmdbCollectionPartDto>? Parts { get; set; }
}
