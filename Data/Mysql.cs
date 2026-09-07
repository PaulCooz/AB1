using Microsoft.EntityFrameworkCore;

namespace AB1.Data;

public static class Mysql
{
    public static DbContextOptionsBuilder UseMariaDb(
        this DbContextOptionsBuilder options,
        string connectionString,
        ServerVersion? serverVersion = null)
    {
        // Oracle-провайдер на MariaDB падает в Migrate() с DBNull→Int64 на GET_LOCK.
        return options.UseMySql(connectionString, serverVersion ?? ServerVersion.AutoDetect(connectionString));
    }

    public static DbContextOptionsBuilder<TContext> UseMariaDb<TContext>(
        this DbContextOptionsBuilder<TContext> options,
        string connectionString,
        ServerVersion? serverVersion = null)
        where TContext : DbContext
    {
        return options.UseMySql(connectionString, serverVersion ?? ServerVersion.AutoDetect(connectionString));
    }
}
