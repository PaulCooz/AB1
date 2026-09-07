using MySqlConnector;

namespace AB1.Data;

public static class DatabaseInitializer
{
    public static async Task EnsureCreatedAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString);
        var database = builder.Database;
        if (string.IsNullOrWhiteSpace(database))
        {
            throw new InvalidOperationException("В строке подключения не указана база данных.");
        }

        // MySQL не даёт открыть соединение к несуществующей БД,
        // поэтому сначала подключаемся к серверу без имени схемы.
        builder.Database = string.Empty;

        await using var connection = new MySqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        var safeName = database.Replace("`", "``", StringComparison.Ordinal);
        command.CommandText =
            $"CREATE DATABASE IF NOT EXISTS `{safeName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
