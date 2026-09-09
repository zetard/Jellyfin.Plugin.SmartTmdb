using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Movie = MediaBrowser.Controller.Entities.Movies.Movie;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.SmartTmdb.Tests;

public class LocalMovieResolverTests
{
    private class TestMovie : Movie
    {
        public TestMovie(Guid id, int tmdbId)
        {
            Id = id;
            ProviderIds = new Dictionary<string, string> { ["Tmdb"] = tmdbId.ToString() };
        }
    }

    private static Movie CreateMovie(Guid id, int tmdbId)
    {
        return new TestMovie(id, tmdbId);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsMovies_WithMatchingTmdbIds()
    {
        var movie1 = CreateMovie(Guid.NewGuid(), 101);
        var movie2 = CreateMovie(Guid.NewGuid(), 102);

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns(new List<BaseItem> { movie1, movie2 });

        var userManager = new Mock<IUserManager>();
        var userDataManager = new Mock<IUserDataManager>();

        var resolver = new LocalMovieResolver(libraryManager.Object, userManager.Object, userDataManager.Object);
        var result = await resolver.ResolveAsync(new List<int> { 101, 102 }, null, new Guid[0], CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey(101));
        Assert.True(result.ContainsKey(102));
    }

    [Fact]
    public async Task ResolveAsync_ExcludesNonMatchingTmdbIds()
    {
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns(new List<BaseItem>());

        var userManager = new Mock<IUserManager>();
        var userDataManager = new Mock<IUserDataManager>();

        var resolver = new LocalMovieResolver(libraryManager.Object, userManager.Object, userDataManager.Object);
        var result = await resolver.ResolveAsync(new List<int> { 999 }, null, new Guid[0], CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ResolveAsync_AttachesUserData_WhenUserIdProvided()
    {
        var movie1 = CreateMovie(Guid.NewGuid(), 101);
        var userId = Guid.NewGuid();
        var user = new User(userId.ToString(), "test", "test@example.com");
        var userData = new UserItemData { Key = "0", Played = true };

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns(new List<BaseItem> { movie1 });

        var userManager = new Mock<IUserManager>();
        userManager.Setup(m => m.GetUserById(userId)).Returns(user);

        var userDataManager = new Mock<IUserDataManager>();
        userDataManager.Setup(m => m.GetUserDataBatch(It.IsAny<IReadOnlyList<BaseItem>>(), user))
            .Returns(new Dictionary<Guid, UserItemData> { [movie1.Id] = userData });

        var resolver = new LocalMovieResolver(libraryManager.Object, userManager.Object, userDataManager.Object);
        var result = await resolver.ResolveAsync(new List<int> { 101 }, userId, new Guid[0], CancellationToken.None);

        Assert.Single(result);
        Assert.True(result[101].UserData?.Played);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsEmpty_WhenNoMovies()
    {
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns(new List<BaseItem>());

        var userManager = new Mock<IUserManager>();
        var userDataManager = new Mock<IUserDataManager>();

        var resolver = new LocalMovieResolver(libraryManager.Object, userManager.Object, userDataManager.Object);
        var result = await resolver.ResolveAsync(new List<int> { 101 }, null, new Guid[0], CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsEmpty_WhenTmdbIdsEmpty()
    {
        var libraryManager = new Mock<ILibraryManager>();
        var userManager = new Mock<IUserManager>();
        var userDataManager = new Mock<IUserDataManager>();

        var resolver = new LocalMovieResolver(libraryManager.Object, userManager.Object, userDataManager.Object);
        var result = await resolver.ResolveAsync(new List<int>(), null, new Guid[0], CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ResolveAsync_PassesExcludeItemIds_ToLibraryQuery()
    {
        var excludedId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var movie1 = CreateMovie(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), 101);

        InternalItemsQuery? capturedQuery = null;
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns((InternalItemsQuery q) => { capturedQuery = q; return new List<BaseItem> { movie1 }; });

        var userManager = new Mock<IUserManager>();
        var userDataManager = new Mock<IUserDataManager>();

        var resolver = new LocalMovieResolver(libraryManager.Object, userManager.Object, userDataManager.Object);
        var result = await resolver.ResolveAsync(
            new List<int> { 101 },
            null,
            new Guid[] { excludedId },
            CancellationToken.None);

        Assert.Single(result);
        Assert.NotNull(capturedQuery);
        Assert.Contains(excludedId, capturedQuery!.ExcludeItemIds);
    }
}
