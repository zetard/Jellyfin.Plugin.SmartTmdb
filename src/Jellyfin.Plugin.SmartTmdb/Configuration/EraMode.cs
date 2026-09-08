namespace Jellyfin.Plugin.SmartTmdb.Configuration;

/// <summary>
/// Era mode.
/// </summary>
public enum EraMode
{
    /// <summary>
    /// Off.
    /// </summary>
    Off,

    /// <summary>
    /// Soft preference.
    /// </summary>
    Soft,

    /// <summary>
    /// Strict 10 years.
    /// </summary>
    Strict10
}
