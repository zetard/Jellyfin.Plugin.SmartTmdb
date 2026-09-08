using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SmartTmdb.Configuration;
using Xunit;

namespace Jellyfin.Plugin.SmartTmdb.Tests;

public class MemoryRawTmdbCacheTests
{
    [Fact]
    public async Task GetAsync_ReturnsNull_WhenKeyMissing()
    {
        MemoryRawTmdbCache cache = new MemoryRawTmdbCache();
        string? value = await cache.GetAsync("missing", CancellationToken.None);
        Assert.Null(value);
    }

    [Fact]
    public async Task SetThenGet_ReturnsValue()
    {
        MemoryRawTmdbCache cache = new MemoryRawTmdbCache();
        await cache.SetAsync("key", "value", TimeSpan.FromHours(1), CancellationToken.None);
        string? value = await cache.GetAsync("key", CancellationToken.None);
        Assert.Equal("value", value);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_AfterExpiry()
    {
        MemoryRawTmdbCache cache = new MemoryRawTmdbCache();
        await cache.SetAsync("key", "value", TimeSpan.FromMilliseconds(1), CancellationToken.None);
        await Task.Delay(50);
        string? value = await cache.GetAsync("key", CancellationToken.None);
        Assert.Null(value);
    }

    [Fact]
    public async Task MaxEntries_EvictsExpired()
    {
        MemoryRawTmdbCache cache = new MemoryRawTmdbCache(maxEntries: 2);
        await cache.SetAsync("a", "1", TimeSpan.FromHours(1), CancellationToken.None);
        await cache.SetAsync("b", "2", TimeSpan.FromHours(1), CancellationToken.None);
        await cache.SetAsync("c", "3", TimeSpan.FromHours(1), CancellationToken.None);

        string? a = await cache.GetAsync("a", CancellationToken.None);
        string? b = await cache.GetAsync("b", CancellationToken.None);
        string? c = await cache.GetAsync("c", CancellationToken.None);

        Assert.Null(a);
        Assert.NotNull(b);
        Assert.NotNull(c);
    }

    [Fact]
    public async Task Cancellation_DoesNotThrow()
    {
        MemoryRawTmdbCache cache = new MemoryRawTmdbCache();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await cache.SetAsync("key", "value", TimeSpan.FromHours(1), cts.Token);
        Assert.NotNull(cache);
    }
}