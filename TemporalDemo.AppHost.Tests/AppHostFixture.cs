using System.Text.Json;

namespace TemporalDemo.AppHost.Tests;

public sealed class AppHostFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);

    private DistributedApplication App { get; set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.TemporalDemo_AppHost>();

        builder.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        App = await builder.BuildAsync().WaitAsync(StartupTimeout);
        await App.StartAsync().WaitAsync(StartupTimeout);
    }

    public async Task DisposeAsync()
    {
        await App.DisposeAsync();
    }

    public async Task WaitForResourceHealthyAsync(string resourceName, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(StartupTimeout);

        await App.ResourceNotifications.WaitForResourceHealthyAsync(resourceName, timeoutCts.Token);
    }

    public HttpClient CreateHttpClient(string resourceName, string endpointName = "http") =>
        App.CreateHttpClient(resourceName, endpointName);

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: cancellationToken);
    }
}