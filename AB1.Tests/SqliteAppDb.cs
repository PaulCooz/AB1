using AB1.Data;
using AB1.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AB1.Tests;

/// <summary>
/// SQLite в памяти, чтобы тесты не требовали запущенной MariaDB.
/// Соединение держим открытым: иначе :memory: база исчезает вместе с ним.
/// </summary>
internal sealed class SqliteAppDb : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    public IDbContextFactory<AppDbContext> Factory { get; }

    public UrlShortener Shortener { get; }

    public SqliteAppDb()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        Factory = new DbFactory(options);
        using var db = Factory.CreateDbContext();
        db.Database.EnsureCreated();

        Shortener = new UrlShortener(Factory);
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();

    private sealed class DbFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }
}
