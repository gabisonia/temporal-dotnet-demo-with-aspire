using Microsoft.Extensions.Logging.Abstractions;

namespace TemporalDemo.AppHost.Tests;

public sealed class EnvironmentVariableTests
{
    [Fact]
    public async Task ShopApiPublishesExpectedTemporalEnvironmentVariables()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.TemporalDemo_AppHost>();
        var shopApi = builder.CreateResourceBuilder<ProjectResource>("shop-api");

        var executionConfiguration = await ExecutionConfigurationBuilder.Create(shopApi.Resource)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(new(DistributedApplicationOperation.Publish), NullLogger.Instance, CancellationToken.None)
            .ConfigureAwait(true);

        var environmentVariables = executionConfiguration.EnvironmentVariables.ToDictionary();

        Assert.Contains(environmentVariables, entry =>
            entry.Key == "Temporal__Namespace" && entry.Value == "default");

        Assert.Contains(environmentVariables, entry =>
            entry.Key == "Temporal__Address"
            && entry.Value.Contains("temporal.bindings.grpc", StringComparison.Ordinal));
    }
}
