using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SmartTmdb.Configuration;
using Jellyfin.Plugin.SmartTmdb.Tmdb;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.SmartTmdb.Tests;

public class TmdbClientTests
{
    private const string FakeToken = "fake-token";

    private static ITmdbClient CreateClient(HttpResponseMessage? response, out Mock<IPluginSettingsAccessor> settingsMock, out Mock<IRawTmdbCache> cacheMock)
    {
        settingsMock = new Mock<IPluginSettingsAccessor>();
        settingsMock.Setup(s => s.GetConfiguration()).Returns(new PluginConfiguration { ApiReadAccessToken = FakeToken, ResponseLanguage = "en-US", RequestTimeoutSeconds = 10, RawCacheHours = 1 });

        var handler = new FakeHandler(response);
        var httpClientFactory = new FakeHttpClientFactory(handler);

        cacheMock = new Mock<IRawTmdbCache>();
        cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult<string?>(null));
        cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var loggerMock = new Mock<ILogger<TmdbClient>>();

        return new TmdbClient(httpClientFactory, loggerMock.Object, cacheMock.Object, settingsMock.Object);
    }

    [Fact]
    public async Task GetMovieDetailsAsync_ReturnsDetails()
    {
        var dto = new TmdbMovieDetailsDto { Id = 123, Title = "Test" };
        var json = JsonSerializer.Serialize(dto);

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        ITmdbClient client = CreateClient(response, out Mock<IPluginSettingsAccessor> settingsMock, out Mock<IRawTmdbCache> cacheMock);
        TmdbMovieDetailsDto result = await client.GetMovieDetailsAsync(123, "en-US", CancellationToken.None);

        Assert.Equal(123, result.Id);
        Assert.Equal("Test", result.Title);
    }

    [Fact]
    public async Task GetRecommendationsAsync_ReturnsPage()
    {
        var dto = new TmdbRecommendationPageDto { Page = 1, TotalPages = 3 };
        var json = JsonSerializer.Serialize(dto);

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        ITmdbClient client = CreateClient(response, out Mock<IPluginSettingsAccessor> settingsMock, out Mock<IRawTmdbCache> cacheMock);
        TmdbRecommendationPageDto result = await client.GetRecommendationsAsync(123, "en-US", 1, CancellationToken.None);

        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task GetSimilarAsync_ReturnsPage()
    {
        var dto = new TmdbSimilarPageDto { Page = 1, TotalPages = 3 };
        var json = JsonSerializer.Serialize(dto);

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        ITmdbClient client = CreateClient(response, out Mock<IPluginSettingsAccessor> settingsMock, out Mock<IRawTmdbCache> cacheMock);
        TmdbSimilarPageDto result = await client.GetSimilarAsync(123, "en-US", 1, CancellationToken.None);

        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task GetCollectionAsync_ReturnsCollection()
    {
        var dto = new TmdbCollectionDto { Id = 1, Name = "Collection" };
        var json = JsonSerializer.Serialize(dto);

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        ITmdbClient client = CreateClient(response, out Mock<IPluginSettingsAccessor> settingsMock, out Mock<IRawTmdbCache> cacheMock);
        TmdbCollectionDto result = await client.GetCollectionAsync(1, "en-US", CancellationToken.None);

        Assert.Equal("Collection", result.Name);
    }

    [Fact]
    public async Task GetMovieDetailsAsync_MissingToken_ThrowsUnauthorized()
    {
        var settingsMock = new Mock<IPluginSettingsAccessor>();
        settingsMock.Setup(s => s.GetConfiguration()).Returns(new PluginConfiguration { ApiReadAccessToken = string.Empty });

        var loggerMock = new Mock<ILogger<TmdbClient>>();
        var cacheMock = new Mock<IRawTmdbCache>();
        var httpClientFactory = new FakeHttpClientFactory(new FakeHandler(new HttpResponseMessage(HttpStatusCode.OK)));

        TmdbClient client = new TmdbClient(httpClientFactory, loggerMock.Object, cacheMock.Object, settingsMock.Object);

        await Assert.ThrowsAsync<HttpStatusCodeException>(async () => await client.GetMovieDetailsAsync(123, "en-US", CancellationToken.None));
    }

    [Fact]
    public async Task GetMovieDetailsAsync_401Response_ThrowsUnauthorized()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("Unauthorized") };
        ITmdbClient client = CreateClient(response, out Mock<IPluginSettingsAccessor> settingsMock, out Mock<IRawTmdbCache> cacheMock);

        await Assert.ThrowsAsync<HttpStatusCodeException>(async () => await client.GetMovieDetailsAsync(123, "en-US", CancellationToken.None));
    }

    [Fact]
    public async Task GetMovieDetailsAsync_404Response_ThrowsNotFound()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("Not Found") };
        ITmdbClient client = CreateClient(response, out Mock<IPluginSettingsAccessor> settingsMock, out Mock<IRawTmdbCache> cacheMock);

        await Assert.ThrowsAsync<HttpStatusCodeException>(async () => await client.GetMovieDetailsAsync(123, "en-US", CancellationToken.None));
    }

    [Fact]
    public async Task GetMovieDetailsAsync_429Response_ThrowsTooManyRequests()
    {
        var response = new HttpResponseMessage((HttpStatusCode)429) { Content = new StringContent("Rate limited") };
        ITmdbClient client = CreateClient(response, out Mock<IPluginSettingsAccessor> settingsMock, out Mock<IRawTmdbCache> cacheMock);

        await Assert.ThrowsAsync<HttpStatusCodeException>(async () => await client.GetMovieDetailsAsync(123, "en-US", CancellationToken.None));
    }

    [Fact]
    public async Task GetMovieDetailsAsync_500Response_ThrowsServerError()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("Error") };
        ITmdbClient client = CreateClient(response, out Mock<IPluginSettingsAccessor> settingsMock, out Mock<IRawTmdbCache> cacheMock);

        await Assert.ThrowsAsync<HttpStatusCodeException>(async () => await client.GetMovieDetailsAsync(123, "en-US", CancellationToken.None));
    }

    [Fact]
    public async Task GetMovieDetailsAsync_MalformedJson_ThrowsJsonException()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("not json") };
        ITmdbClient client = CreateClient(response, out Mock<IPluginSettingsAccessor> settingsMock, out Mock<IRawTmdbCache> cacheMock);

        await Assert.ThrowsAsync<JsonException>(async () => await client.GetMovieDetailsAsync(123, "en-US", CancellationToken.None));
    }

    [Fact]
    public async Task GetMovieDetailsAsync_CachesSuccessfulResponse()
    {
        var dto = new TmdbMovieDetailsDto { Id = 123, Title = "Cached" };
        var json = JsonSerializer.Serialize(dto);

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        Mock<IRawTmdbCache> cacheMock;
        ITmdbClient client = CreateClient(response, out Mock<IPluginSettingsAccessor> settingsMock, out cacheMock);

        TmdbMovieDetailsDto result1 = await client.GetMovieDetailsAsync(123, "en-US", CancellationToken.None);
        cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), json, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);

        cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult<string?>(json));
        TmdbMovieDetailsDto result2 = await client.GetMovieDetailsAsync(123, "en-US", CancellationToken.None);

        Assert.Equal(result1.Title, result2.Title);
    }

    [Fact]
    public async Task GetMovieDetailsAsync_TokenNotLoggedInUri()
    {
        var dto = new TmdbMovieDetailsDto { Id = 123 };
        var json = JsonSerializer.Serialize(dto);

        var capturedUri = string.Empty;
        var handler = new FakeHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });
        handler.RequestCaptured = req =>
        {
            capturedUri = req.RequestUri?.ToString() ?? string.Empty;
        };

        var httpClientFactory = new FakeHttpClientFactory(handler);
        var settingsMock = new Mock<IPluginSettingsAccessor>();
        settingsMock.Setup(s => s.GetConfiguration()).Returns(new PluginConfiguration { ApiReadAccessToken = FakeToken, ResponseLanguage = "en-US", RequestTimeoutSeconds = 10, RawCacheHours = 1 });
        var loggerMock = new Mock<ILogger<TmdbClient>>();
        var cacheMock = new Mock<IRawTmdbCache>();

        TmdbClient client = new TmdbClient(httpClientFactory, loggerMock.Object, cacheMock.Object, settingsMock.Object);
        await client.GetMovieDetailsAsync(123, "en-US", CancellationToken.None);

        Assert.DoesNotContain(FakeToken, capturedUri);
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public FakeHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public Action<HttpRequestMessage>? RequestCaptured { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCaptured?.Invoke(request);
            return Task.FromResult(_response);
        }
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public FakeHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(_handler, disposeHandler: false);
        }
    }
}