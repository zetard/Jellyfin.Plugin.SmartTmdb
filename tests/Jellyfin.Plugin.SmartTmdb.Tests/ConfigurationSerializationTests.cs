using System.IO;
using System.Text;
using Jellyfin.Plugin.SmartTmdb.Configuration;
using Xunit;

namespace Jellyfin.Plugin.SmartTmdb.Tests;

/// <summary>
/// Regression tests for plugin configuration persistence.
/// Jellyfin's <c>BasePluginOfT.SaveConfiguration</c> persists the plugin
/// configuration using <see cref="System.Xml.XmlSerializer"/> (the
/// <see cref="MediaBrowser.Model.Serialization.IXmlSerializer"/> implementation),
/// which requires every persisted type to have a public parameterless
/// constructor. A <c>record</c> with positional parameters has none, so any
/// save that included a populated <see cref="ScoringWeights"/> threw an
/// <see cref="System.InvalidOperationException"/> that surfaced as an HTTP
/// 500 on <c>POST /Plugins/{id}/Configuration</c>.
/// </summary>
public class ConfigurationSerializationTests
{
    [Fact]
    public void ScoringWeights_RoundTripsThroughXmlSerializer()
    {
        ScoringWeights original = new(0.35, 0.25, 0.20, 0.10, 0.05, 0.05);
        System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ScoringWeights));

        string xml = Serialize(serializer, original);
        ScoringWeights? deserialized = (ScoringWeights?)serializer.Deserialize(new StringReader(xml));

        Assert.NotNull(deserialized);
        Assert.Equal(original.RecommendationRank, deserialized.RecommendationRank);
        Assert.Equal(original.SimilarRank, deserialized.SimilarRank);
        Assert.Equal(original.Genre, deserialized.Genre);
        Assert.Equal(original.Era, deserialized.Era);
        Assert.Equal(original.Quality, deserialized.Quality);
        Assert.Equal(original.Popularity, deserialized.Popularity);
    }

    [Fact]
    public void PluginConfiguration_RoundTripsThroughXmlSerializer()
    {
        PluginConfiguration original = new()
        {
            ApiReadAccessToken = "token",
            Preset = RecommendationPreset.Custom,
            CustomWeights = new ScoringWeights(0.55, 0.15, 0.10, 0.05, 0.10, 0.05),
            MinimumVoteAverage = 7.0,
            MinimumVoteCount = 50,
        };

        System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(PluginConfiguration));

        string xml = Serialize(serializer, original);
        PluginConfiguration? deserialized = (PluginConfiguration?)serializer.Deserialize(new StringReader(xml));

        Assert.NotNull(deserialized);
        Assert.Equal(original.Preset, deserialized.Preset);
        Assert.Equal(original.MinimumVoteAverage, deserialized.MinimumVoteAverage);
        Assert.Equal(original.MinimumVoteCount, deserialized.MinimumVoteCount);
        Assert.NotNull(deserialized.CustomWeights);
        Assert.Equal(original.CustomWeights.RecommendationRank, deserialized.CustomWeights.RecommendationRank);
        Assert.Equal(original.CustomWeights.Popularity, deserialized.CustomWeights.Popularity);
    }

    private static string Serialize(System.Xml.Serialization.XmlSerializer serializer, object value)
    {
        var sb = new StringBuilder();
        using (var writer = new StringWriter(sb, System.Globalization.CultureInfo.InvariantCulture))
        {
            serializer.Serialize(writer, value);
        }

        return sb.ToString();
    }
}