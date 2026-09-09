using System;
using Jellyfin.Plugin.SmartTmdb.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// Plugin settings accessor.
/// </summary>
public sealed class PluginSettingsAccessor : IPluginSettingsAccessor
{
    private const string EnvironmentTokenVariable = "JELLYFIN_SMART_TMDB_TOKEN";
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

    /// <summary>
    /// Resolves the TMDB API token with environment variable precedence.
    /// </summary>
    /// <param name="configuration">Plugin configuration.</param>
    /// <returns>Resolved token, or null if not available.</returns>
    public static string? ResolveApiReadAccessToken(PluginConfiguration configuration)
    {
        string? environmentToken = Environment.GetEnvironmentVariable(EnvironmentTokenVariable);
        if (!string.IsNullOrWhiteSpace(environmentToken))
        {
            return environmentToken.Trim();
        }

        if (!string.IsNullOrWhiteSpace(configuration.ApiReadAccessToken))
        {
            return configuration.ApiReadAccessToken.Trim();
        }

        return null;
    }

    /// <summary>
    /// Gets a value indicating whether the token is supplied by the environment variable.
    /// </summary>
    /// <returns>True if the environment variable is set; otherwise false.</returns>
    public static bool IsTokenFromEnvironment()
    {
        return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvironmentTokenVariable));
    }
}
