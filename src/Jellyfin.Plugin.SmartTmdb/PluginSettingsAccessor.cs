using Jellyfin.Plugin.SmartTmdb.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// Plugin settings accessor.
/// </summary>
public sealed class PluginSettingsAccessor : IPluginSettingsAccessor
{
    private readonly ILogger<PluginSettingsAccessor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginSettingsAccessor"/> class.
    /// </summary>
    /// <param name="logger">Logger.</param>
    public PluginSettingsAccessor(ILogger<PluginSettingsAccessor> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public PluginConfiguration GetConfiguration()
    {
        return new PluginConfiguration();
    }
}
