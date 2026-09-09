using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Movie = MediaBrowser.Controller.Entities.Movies.Movie;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// Plugin service registrator.
/// </summary>
public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc/>
    public void RegisterServices(IServiceCollection services, IServerApplicationHost applicationHost)
    {
        services.AddSingleton<IPluginSettingsAccessor, PluginSettingsAccessor>();
        services.AddSingleton<IRawTmdbCache, MemoryRawTmdbCache>();
        services.AddTransient<ITmdbClient, TmdbClient>();
        services.AddTransient<ILocalMovieResolver, LocalMovieResolver>();
        services.AddSingleton<ICandidateScorer, CandidateScorer>();
        services.AddTransient<IRemoteSimilarItemsProvider<Movie>, SmartTmdbMovieProvider>();
    }
}
