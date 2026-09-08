using Jellyfin.Plugin.SmartTmdb.Configuration;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// Accessor for plugin settings.
/// </summary>
public interface IPluginSettingsAccessor
{
    /// <summary>
    /// Gets the current configuration snapshot.
    /// </summary>
    PluginConfiguration GetConfiguration();
}
