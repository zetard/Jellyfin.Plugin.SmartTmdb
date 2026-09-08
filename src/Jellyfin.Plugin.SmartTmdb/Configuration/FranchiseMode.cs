namespace Jellyfin.Plugin.SmartTmdb.Configuration;

/// <summary>
/// Franchise mode.
/// </summary>
public enum FranchiseMode
{
    /// <summary>
    /// Prefer next collection part.
    /// </summary>
    PreferNext,

    /// <summary>
    /// Neutral mode.
    /// </summary>
    Neutral,

    /// <summary>
    /// Avoid same collection.
    /// </summary>
    Avoid
}
