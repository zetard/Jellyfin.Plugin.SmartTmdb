using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SmartTmdb.Configuration;
using Jellyfin.Plugin.SmartTmdb.Tmdb;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// TMDB client implementation using IHttpClientFactory.
/// </summary>
public sealed class TmdbClient : ITmdbClient
{
    private const string BaseAddress = "https://api.themoviedb.org/3/";
    private const string BearerPrefix = "Bearer ";
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TmdbClient> _logger;
    private readonly IRawTmdbCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="TmdbClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="cache">Raw cache.</param>
    /// <param name="settingsAccessor">Settings accessor.</param>
    public TmdbClient(
        IHttpClientFactory httpClientFactory,
        ILogger<TmdbClient> logger,
        IRawTmdbCache cache,
        IPluginSettingsAccessor settingsAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _cache = cache;

        ArgumentNullException.ThrowIfNull(settingsAccessor);
        _settingsAccessor = settingsAccessor;
    }

    private readonly IPluginSettingsAccessor _settingsAccessor;

    /// <inheritdoc/>
    public async Task<TmdbMovieDetailsDto> GetMovieDetailsAsync(int tmdbId, string? language, CancellationToken cancellationToken)
    {
        PluginConfiguration config = _settingsAccessor.GetConfiguration();
        string? token = PluginSettingsAccessor.ResolveApiReadAccessToken(config);

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new HttpStatusCodeException(System.Net.HttpStatusCode.Unauthorized, "TMDB token is not configured.");
        }

        string lang = ResolveLanguage(language, config.ResponseLanguage);
        string url = $"movie/{tmdbId}?language={Uri.EscapeDataString(lang)}&append_to_response=recommendations,similar";

        using HttpResponseMessage response = await SendAsync(url, token, config, cancellationToken).ConfigureAwait(false);
        string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        TmdbMovieDetailsDto? dto = JsonSerializer.Deserialize<TmdbMovieDetailsDto>(json, JsonOptions);
        return dto ?? throw new JsonException("Failed to deserialize movie details.");
    }

    /// <inheritdoc/>
    public async Task<TmdbRecommendationPageDto> GetRecommendationsAsync(int tmdbId, string? language, int page, CancellationToken cancellationToken)
    {
        PluginConfiguration config = _settingsAccessor.GetConfiguration();
        string? token = PluginSettingsAccessor.ResolveApiReadAccessToken(config);

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new HttpStatusCodeException(System.Net.HttpStatusCode.Unauthorized, "TMDB token is not configured.");
        }

        string lang = ResolveLanguage(language, config.ResponseLanguage);
        string url = $"movie/{tmdbId}/recommendations?language={Uri.EscapeDataString(lang)}&page={page}";

        using HttpResponseMessage response = await SendAsync(url, token, config, cancellationToken).ConfigureAwait(false);
        string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        TmdbRecommendationPageDto? dto = JsonSerializer.Deserialize<TmdbRecommendationPageDto>(json, JsonOptions);
        return dto ?? throw new JsonException("Failed to deserialize recommendations page.");
    }

    /// <inheritdoc/>
    public async Task<TmdbSimilarPageDto> GetSimilarAsync(int tmdbId, string? language, int page, CancellationToken cancellationToken)
    {
        PluginConfiguration config = _settingsAccessor.GetConfiguration();
        string? token = PluginSettingsAccessor.ResolveApiReadAccessToken(config);

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new HttpStatusCodeException(System.Net.HttpStatusCode.Unauthorized, "TMDB token is not configured.");
        }

        string lang = ResolveLanguage(language, config.ResponseLanguage);
        string url = $"movie/{tmdbId}/similar?language={Uri.EscapeDataString(lang)}&page={page}";

        using HttpResponseMessage response = await SendAsync(url, token, config, cancellationToken).ConfigureAwait(false);
        string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        TmdbSimilarPageDto? dto = JsonSerializer.Deserialize<TmdbSimilarPageDto>(json, JsonOptions);
        return dto ?? throw new JsonException("Failed to deserialize similar movies page.");
    }

    /// <inheritdoc/>
    public async Task<TmdbCollectionDto> GetCollectionAsync(int collectionId, string? language, CancellationToken cancellationToken)
    {
        PluginConfiguration config = _settingsAccessor.GetConfiguration();
        string? token = PluginSettingsAccessor.ResolveApiReadAccessToken(config);

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new HttpStatusCodeException(System.Net.HttpStatusCode.Unauthorized, "TMDB token is not configured.");
        }

        string lang = ResolveLanguage(language, config.ResponseLanguage);
        string url = $"collection/{collectionId}?language={Uri.EscapeDataString(lang)}";

        using HttpResponseMessage response = await SendAsync(url, token, config, cancellationToken).ConfigureAwait(false);
        string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        TmdbCollectionDto? dto = JsonSerializer.Deserialize<TmdbCollectionDto>(json, JsonOptions);
        return dto ?? throw new JsonException("Failed to deserialize collection.");
    }

    private async Task<HttpResponseMessage> SendAsync(string relativePath, string token, PluginConfiguration config, CancellationToken cancellationToken)
    {
        if (_cache is null)
        {
            return await SendCoreAsync(relativePath, token, config, cancellationToken).ConfigureAwait(false);
        }

        string cacheKey = BuildCacheKey(relativePath);
        string? cached = await _cache.GetAsync(cacheKey, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(cached))
        {
            _logger.LogDebug("TMDB cache hit for {Path}", relativePath);

            using var doc = JsonDocument.Parse(cached);
            var ms = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(cached));
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StreamContent(ms),
            };
        }

        _logger.LogDebug("TMDB cache miss for {Path}", relativePath);
        HttpResponseMessage response = await SendCoreAsync(relativePath, token, config, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            string? body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(body))
            {
                TimeSpan ttl = TimeSpan.FromHours(Math.Clamp(config.RawCacheHours, 1, 168));
                await _cache.SetAsync(cacheKey, body, ttl, cancellationToken).ConfigureAwait(false);
            }
        }

        return response;
    }

    private async Task<HttpResponseMessage> SendCoreAsync(string relativePath, string token, PluginConfiguration config, CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient("SmartTmdb");
        using var request = new HttpRequestMessage(HttpMethod.Get, BaseAddress + relativePath);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Replace(BearerPrefix, string.Empty, StringComparison.OrdinalIgnoreCase).Trim());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Clamp(config.RequestTimeoutSeconds, 1, 60)));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        CancellationToken effectiveToken = linkedCts.Token;

        try
        {
            HttpResponseMessage response = await client.SendAsync(request, effectiveToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                await HandleFailureAsync(relativePath, response, effectiveToken).ConfigureAwait(false);
            }

            return response;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("TMDB request timed out for {Path}", relativePath);
            throw new HttpStatusCodeException(System.Net.HttpStatusCode.GatewayTimeout, "TMDB request timed out.");
        }
    }

    private async Task HandleFailureAsync(string path, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string content = string.Empty;

        try
        {
            content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            content = "<unreadable>";
        }

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning("TMDB rate limited on {Path}: {Status}.", path, response.StatusCode);
            throw new HttpStatusCodeException(response.StatusCode, "TMDB rate limited.");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogWarning("TMDB auth failure on {Path}: {Status}.", path, response.StatusCode);
            throw new HttpStatusCodeException(response.StatusCode, "TMDB authentication failed.");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogDebug("TMDB not found for {Path}.", path);
            throw new HttpStatusCodeException(response.StatusCode, "TMDB resource not found.");
        }

        if ((int)response.StatusCode >= 500)
        {
            _logger.LogWarning("TMDB server error on {Path}: {Status}.", path, response.StatusCode);
            throw new HttpStatusCodeException(response.StatusCode, "TMDB server error.");
        }

        _logger.LogWarning("TMDB request failed for {Path}: {Status} {Body}", path, response.StatusCode, content);
        throw new HttpStatusCodeException(response.StatusCode, $"TMDB request failed: {response.StatusCode}");
    }

    private static string ResolveLanguage(string? requestLanguage, string configLanguage)
    {
        if (!string.IsNullOrWhiteSpace(requestLanguage))
        {
            return requestLanguage.Trim();
        }

        if (!string.IsNullOrWhiteSpace(configLanguage) && !configLanguage.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            return configLanguage.Trim();
        }

        return "en-US";
    }

    private static string BuildCacheKey(string relativePath)
    {
        return $"tmdb:{relativePath}";
    }
}