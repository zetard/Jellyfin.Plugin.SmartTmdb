using System;
using Jellyfin.Plugin.SmartTmdb.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Moq;
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
    public void GetConfiguration_ReturnsSavedConfig_WhenPluginInstanceIsSet()
    {
        var saved = new PluginConfiguration
        {
            ApiReadAccessToken = "saved-token",
            Preset = RecommendationPreset.Mainstream,
            WatchedMode = WatchedMode.UnwatchedOnly,
            MinimumVoteAverage = 7.0,
            MinimumVoteCount = 500,
        };

        var pathsMock = new Mock<IApplicationPaths>();
        pathsMock.Setup(p => p.ProgramDataPath).Returns(Path.Combine(Path.GetTempPath(), "smarttmdb-data"));
        pathsMock.Setup(p => p.WebPath).Returns(Path.Combine(Path.GetTempPath(), "smarttmdb-web"));
        pathsMock.Setup(p => p.ProgramSystemPath).Returns(Path.Combine(Path.GetTempPath(), "smarttmdb-sys"));
        pathsMock.Setup(p => p.DataPath).Returns(Path.Combine(Path.GetTempPath(), "smarttmdb-data"));
        pathsMock.Setup(p => p.ImageCachePath).Returns(Path.Combine(Path.GetTempPath(), "smarttmdb-cache"));
        pathsMock.Setup(p => p.PluginsPath).Returns(Path.Combine(Path.GetTempPath(), "smarttmdb-plugins"));
        pathsMock.Setup(p => p.PluginConfigurationsPath).Returns(Path.Combine(Path.GetTempPath(), "smarttmdb-configs"));
        pathsMock.Setup(p => p.LogDirectoryPath).Returns(Path.Combine(Path.GetTempPath(), "smarttmdb-logs"));
        pathsMock.Setup(p => p.ConfigurationDirectoryPath).Returns(Path.Combine(Path.GetTempPath(), "smarttmdb-cfg"));
        pathsMock.Setup(p => p.SystemConfigurationFilePath).Returns(Path.Combine(Path.GetTempPath(), "smarttmdb-system.xml"));
        pathsMock.Setup(p => p.CachePath).Returns(Path.Combine(Path.GetTempPath(), "smarttmdb-cache"));
        pathsMock.Setup(p => p.TempDirectory).Returns(Path.GetTempPath());
        pathsMock.Setup(p => p.VirtualDataPath).Returns(Path.Combine(Path.GetTempPath(), "smarttmdb-virtual"));
        pathsMock.Setup(p => p.TrickplayPath).Returns(Path.Combine(Path.GetTempPath(), "smarttmdb-trickplay"));
        pathsMock.Setup(p => p.BackupPath).Returns(Path.Combine(Path.GetTempPath(), "smarttmdb-backup"));

        var plugin = new Plugin(
            pathsMock.Object,
            new Mock<IXmlSerializer>().Object);
        typeof(BasePlugin<PluginConfiguration>)
            .GetProperty("Configuration")!
            .SetValue(plugin, saved);
        var accessor = new PluginSettingsAccessor(new Mock<ILogger<PluginSettingsAccessor>>().Object);

        PluginConfiguration result = accessor.GetConfiguration();

        Assert.Equal("saved-token", result.ApiReadAccessToken);
        Assert.Equal(RecommendationPreset.Mainstream, result.Preset);
        Assert.Equal(WatchedMode.UnwatchedOnly, result.WatchedMode);
        Assert.Equal(7.0, result.MinimumVoteAverage);
        Assert.Equal(500, result.MinimumVoteCount);
    }

    [Fact]
    public void GetConfiguration_ReturnsDefaults_WhenPluginInstanceIsNotSet()
    {
        Plugin.Instance = null;
        var accessor = new PluginSettingsAccessor(new Mock<ILogger<PluginSettingsAccessor>>().Object);

        PluginConfiguration result = accessor.GetConfiguration();

        Assert.Equal(string.Empty, result.ApiReadAccessToken);
        Assert.Equal(RecommendationPreset.Balanced, result.Preset);
    }

    [Fact]
    public void IsTokenFromEnvironment_ReturnsFalse_WhenEnvVarEmpty()
    {
        Environment.SetEnvironmentVariable("JELLYFIN_SMART_TMDB_TOKEN", string.Empty);

        bool isFromEnv = PluginSettingsAccessor.IsTokenFromEnvironment();

        Assert.False(isFromEnv);
    }
}