using AB1.Models;
using Microsoft.EntityFrameworkCore;

namespace AB1.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ShortUrl> ShortUrls => Set<ShortUrl>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var shortUrl = modelBuilder.Entity<ShortUrl>();

        // Redirects always look up by code, so a unique index both speeds that up
        // and prevents two different URLs from claiming the same short link.
        shortUrl.HasIndex(x => x.Hash).IsUnique();

        shortUrl.Property(x => x.OriginalUrl)
            .HasMaxLength(2048)
            .IsRequired();

        shortUrl.Property(x => x.Hash)
            .HasMaxLength(32)
            .IsRequired();
    }
}