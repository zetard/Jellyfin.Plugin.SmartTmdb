namespace Jellyfin.Plugin.SmartTmdb.Configuration;

/// <summary>
/// Language mode.
/// </summary>
public enum LanguageMode
{
    /// <summary>
    /// Any language.
    /// </summary>
    Any,

    /// <summary>
    /// Prefer source language.
    /// </summary>
    PreferSource,

    /// <summary>
    /// Only source language.
    /// </summary>
    OnlySource
}
