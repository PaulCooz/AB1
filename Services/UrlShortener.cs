using System.Buffers.Text;
using System.Security.Cryptography;
using AB1.Data;
using AB1.Models;
using Microsoft.EntityFrameworkCore;

namespace AB1.Services;

public sealed class UrlShortener(IDbContextFactory<AppDbContext> dbFactory)
{
    public string NewHash()
    {
        Span<byte> bytes = stackalloc byte[9]; // 9 байт = 12 base64Url
        RandomNumberGenerator.Fill(bytes);
        return Base64Url.EncodeToString(bytes);
    }

    public async Task<ShortUrl> CreateAsync(string? longUrl, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeUrl(longUrl);
        var hash = NewHash();

        if (string.IsNullOrWhiteSpace(hash))
        {
            throw new InvalidOperationException("NewHash должен вернуть непустой код.");
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

    public async Task<IReadOnlyList<ShortUrl>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.ShortUrls
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateOriginalUrlAsync(int id, string? longUrl, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeUrl(longUrl);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.ShortUrls.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                     ?? throw new InvalidOperationException("Запись не найдена.");

        entity.OriginalUrl = normalized;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var deleted = await db.ShortUrls.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken);
        if (deleted == 0)
        {
            throw new InvalidOperationException("Запись не найдена.");
        }
    }

    public static string ToPublicUrl(string baseUri, string hash)
        => $"{baseUri.TrimEnd('/')}/s/{hash}";

    public async Task<string?> ResolveAndCountAsync(string? hash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return null;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var originalUrl = await db.ShortUrls
            .AsNoTracking()
            .Where(x => x.Hash == hash)
            .Select(x => x.OriginalUrl)
            .FirstOrDefaultAsync(cancellationToken);

        if (originalUrl is null)
            return null;

        // Инкремент в одном UPDATE, чтобы два одновременных перехода не перезаписали один и тот же ClickCount.
        await db.ShortUrls
            .Where(x => x.Hash == hash)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ClickCount, x => x.ClickCount + 1), cancellationToken);

        return originalUrl;
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