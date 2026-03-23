using Microsoft.Extensions.Configuration;

namespace TemporalDemo.AppHost;

internal sealed record AppHostSettings(
    TemporalSettings Temporal,
    AppDatabaseSettings AppDatabase)
{
    public static AppHostSettings Load(IConfiguration configuration) =>
        new(
            Temporal: new TemporalSettings(
                Server: new TemporalServerSettings(
                    Db: configuration.GetRequiredValue("Temporal:Server:Db")),
                Client: new TemporalClientSettings(
                    Namespace: configuration["Temporal:Client:Namespace"] ?? "default")),
            AppDatabase: new AppDatabaseSettings(
                Database: configuration.GetRequiredValue("AppDatabase:Database")));
}

internal sealed record TemporalSettings(
    TemporalServerSettings Server,
    TemporalClientSettings Client);

internal sealed record TemporalServerSettings(string Db);

internal sealed record TemporalClientSettings(string Namespace);

internal sealed record AppDatabaseSettings(string Database);

internal static class ConfigurationExtensions
{
    public static string GetRequiredValue(this IConfiguration configuration, string key) =>
        configuration[key] ?? throw new InvalidOperationException($"Configuration value '{key}' is not configured.");

    public static int GetRequiredInt(this IConfiguration configuration, string key) =>
        int.Parse(configuration.GetRequiredValue(key));
}