using System;
using Jellyfin.Plugin.SmartTmdb.Configuration;
using Xunit;

namespace Jellyfin.Plugin.SmartTmdb.Tests;

public class PluginSettingsAccessorTests
{
    private const string EnvToken = "env-token-123";
    private const string ConfigToken = "config-token-456";

    [Fact]
    public void ResolveApiReadAccessToken_EnvVarTakesPrecedence()
    {
        Environment.SetEnvironmentVariable("JELLYFIN_SMART_TMDB_TOKEN", EnvToken);

        string? resolved = PluginSettingsAccessor.ResolveApiReadAccessToken(new PluginConfiguration
        {
            ApiReadAccessToken = ConfigToken
        });

        Assert.Equal(EnvToken, resolved);
    }

    [Fact]
    public void ResolveApiReadAccessToken_FallsBackToConfig_WhenEnvVarEmpty()
    {
        Environment.SetEnvironmentVariable("JELLYFIN_SMART_TMDB_TOKEN", string.Empty);

        string? resolved = PluginSettingsAccessor.ResolveApiReadAccessToken(new PluginConfiguration
        {
            ApiReadAccessToken = ConfigToken
        });

        Assert.Equal(ConfigToken, resolved);
    }

    [Fact]
    public void ResolveApiReadAccessToken_ReturnsNull_WhenNoToken()
    {
        Environment.SetEnvironmentVariable("JELLYFIN_SMART_TMDB_TOKEN", string.Empty);

        string? resolved = PluginSettingsAccessor.ResolveApiReadAccessToken(new PluginConfiguration
        {
            ApiReadAccessToken = string.Empty
        });

        Assert.Null(resolved);
    }

    [Fact]
    public void ResolveApiReadAccessToken_TrimsToken()
    {
        Environment.SetEnvironmentVariable("JELLYFIN_SMART_TMDB_TOKEN", "  " + EnvToken + "  ");

        string? resolved = PluginSettingsAccessor.ResolveApiReadAccessToken(new PluginConfiguration
        {
            ApiReadAccessToken = "  " + ConfigToken + "  "
        });

        Assert.Equal(EnvToken.Trim(), resolved);
    }

    [Fact]
    public void IsTokenFromEnvironment_ReturnsTrue_WhenEnvVarSet()
    {
        Environment.SetEnvironmentVariable("JELLYFIN_SMART_TMDB_TOKEN", EnvToken);

        bool isFromEnv = PluginSettingsAccessor.IsTokenFromEnvironment();

        Assert.True(isFromEnv);
    }

    [Fact]
    public void IsTokenFromEnvironment_ReturnsFalse_WhenEnvVarEmpty()
    {
        Environment.SetEnvironmentVariable("JELLYFIN_SMART_TMDB_TOKEN", string.Empty);

        bool isFromEnv = PluginSettingsAccessor.IsTokenFromEnvironment();

        Assert.False(isFromEnv);
    }
}