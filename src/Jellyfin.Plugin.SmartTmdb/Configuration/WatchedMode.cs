namespace Jellyfin.Plugin.SmartTmdb.Configuration;

/// <summary>
/// Watched mode.
/// </summary>
public enum WatchedMode
{
    /// <summary>
    /// Allow watched movies.
    /// </summary>
    Allow,

    /// <summary>
    /// Prefer unwatched movies.
    /// </summary>
    PreferUnwatched,

    /// <summary>
    /// Only unwatched movies.
    /// </summary>
    UnwatchedOnly
}
