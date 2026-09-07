using AB1.Models;
using AB1.Services;

namespace AB1.Tests;

public sealed class UrlShortenerTests : IAsyncLifetime
{
    private SqliteAppDb _db = null!;
    private UrlShortener _sut = null!;

    public Task InitializeAsync()
    {
        _db = new SqliteAppDb();
        _sut = _db.Shortener;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/file")]
    public async Task CreateAsync_RejectsInvalidUrl(string? url)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(url));
    }

    [Fact]
    public async Task CreateAsync_TrimsAndSavesHttpUrl()
    {
        var created = await _sut.CreateAsync("  https://example.com/path  ");

        Assert.Equal("https://example.com/path", created.OriginalUrl);
        Assert.False(string.IsNullOrWhiteSpace(created.Hash));
        Assert.Equal(0, created.ClickCount);

        var listed = await _sut.ListAsync();
        Assert.Single(listed);
        Assert.Equal(created.Hash, listed[0].Hash);
    }

    [Fact]
    public async Task UpdateOriginalUrlAsync_ChangesLongUrlOnly()
    {
        var created = await _sut.CreateAsync("https://example.com/old");

        await _sut.UpdateOriginalUrlAsync(created.Id, "https://example.com/new");

        var listed = await _sut.ListAsync();
        Assert.Single(listed);
        Assert.Equal("https://example.com/new", listed[0].OriginalUrl);
        Assert.Equal(created.Hash, listed[0].Hash);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRow()
    {
        var created = await _sut.CreateAsync("https://example.com/gone");

        await _sut.DeleteAsync(created.Id);

        Assert.Empty(await _sut.ListAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DeleteAsync(created.Id));
    }

    [Fact]
    public async Task ResolveAndCountAsync_IncrementsClickCount()
    {
        await SeedAsync("ab+c/d=", "https://example.com/target");

        var first = await _sut.ResolveAndCountAsync("ab+c/d=");
        var second = await _sut.ResolveAndCountAsync(Uri.EscapeDataString("ab+c/d="));

        Assert.Equal("https://example.com/target", first);
        Assert.Equal(first, second);
        Assert.Equal(2, (await _sut.ListAsync())[0].ClickCount);
    }

    [Fact]
    public async Task ResolveAndCountAsync_UnknownHash_ReturnsNull()
    {
        Assert.Null(await _sut.ResolveAndCountAsync("missing"));
        Assert.Null(await _sut.ResolveAndCountAsync(null));
    }

    [Fact]
    public void ToPublicUrl_EncodesReservedCharacters()
    {
        var url = UrlShortener.ToPublicUrl("http://localhost:5218/", "a+b/c=");

        Assert.Equal("http://localhost:5218/s/a%2Bb%2Fc%3D", url);
    }

    private async Task SeedAsync(string hash, string originalUrl)
    {
        await using var db = await _db.Factory.CreateDbContextAsync();
        db.ShortUrls.Add(new ShortUrl
        {
            OriginalUrl = originalUrl,
            Hash = hash,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
