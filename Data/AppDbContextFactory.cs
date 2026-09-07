using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AB1.Data;

/// <summary>
/// Нужен инструментам EF: они читают модель без запуска сайта и без живой БД.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("Default") ??
                               throw new InvalidOperationException("Задайте ConnectionStrings:Default.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMariaDb(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}