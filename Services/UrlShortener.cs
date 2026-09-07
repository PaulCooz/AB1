using AB1.Data;
using AB1.Models;
using Microsoft.EntityFrameworkCore;

namespace AB1.Services;

public sealed class UrlShortener(IDbContextFactory<AppDbContext> dbFactory)
{
    public string Compress(string longUrl)
    {
        var hash = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        foreach (var c in longUrl)
            hash = HashCode.Combine(hash, c);
        return Convert.ToBase64String(BitConverter.GetBytes(hash));
    }

    public async Task<ShortUrl> CreateAsync(string? longUrl, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeUrl(longUrl);
        var hash = Compress(normalized);

        if (string.IsNullOrWhiteSpace(hash))
        {
            throw new InvalidOperationException("Compress должен вернуть непустой код.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var entity = new ShortUrl
        {
            OriginalUrl = normalized,
            Hash = hash,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.ShortUrls.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static string NormalizeUrl(string? longUrl)
    {
        if (string.IsNullOrWhiteSpace(longUrl))
        {
            throw new ArgumentException("Укажите URL.");
        }

        if (!Uri.TryCreate(longUrl.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Нужен абсолютный URL со схемой http или https.");
        }

        return uri.AbsoluteUri;
    }
}