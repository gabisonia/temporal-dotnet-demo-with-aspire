using Microsoft.Extensions.Configuration;

namespace TemporalDemo.AppHost;

internal sealed record AppHostSettings(
    TemporalSettings Temporal,
    AppDatabaseSettings AppDatabase)
{
    public static AppHostSettings Load(IConfiguration configuration) =>
        new(
            Temporal: new TemporalSettings(
                Postgres: new TemporalPostgresSettings(
                    User: configuration.GetRequiredValue("Temporal:Postgres:User"),
                    Password: configuration.GetRequiredValue("Temporal:Postgres:Password"),
                    Database: configuration.GetRequiredValue("Temporal:Postgres:Database")),
                Server: new TemporalServerSettings(
                    Db: configuration.GetRequiredValue("Temporal:Server:Db"),
                    DbPort: configuration.GetRequiredValue("Temporal:Server:DbPort"),
                    PostgresSeeds: configuration.GetRequiredValue("Temporal:Server:PostgresSeeds"),
                    GrpcPort: configuration.GetRequiredInt("Temporal:Server:GrpcPort")),
                Ui: new TemporalUiSettings(
                    Port: configuration.GetRequiredInt("Temporal:Ui:Port")),
                Client: new TemporalClientSettings(
                    Address: configuration["Temporal:Client:Address"]
                             ?? $"localhost:{configuration.GetRequiredInt("Temporal:Server:GrpcPort")}",
                    Namespace: configuration["Temporal:Client:Namespace"] ?? "default")),
            AppDatabase: new AppDatabaseSettings(
                User: configuration.GetRequiredValue("AppDatabase:User"),
                Password: configuration.GetRequiredValue("AppDatabase:Password"),
                Database: configuration.GetRequiredValue("AppDatabase:Database"),
                Port: configuration.GetRequiredInt("AppDatabase:Port")));
}

internal sealed record TemporalSettings(
    TemporalPostgresSettings Postgres,
    TemporalServerSettings Server,
    TemporalUiSettings Ui,
    TemporalClientSettings Client);

internal sealed record TemporalPostgresSettings(string User, string Password, string Database);

internal sealed record TemporalServerSettings(string Db, string DbPort, string PostgresSeeds, int GrpcPort);

internal sealed record TemporalUiSettings(int Port);

internal sealed record TemporalClientSettings(string Address, string Namespace);

internal sealed record AppDatabaseSettings(string User, string Password, string Database, int Port);

internal static class ConfigurationExtensions
{
    public static string GetRequiredValue(this IConfiguration configuration, string key) =>
        configuration[key] ?? throw new InvalidOperationException($"Configuration value '{key}' is not configured.");

    public static int GetRequiredInt(this IConfiguration configuration, string key) =>
        int.Parse(configuration.GetRequiredValue(key));
}
