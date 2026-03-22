namespace TemporalDemo.AppHost.Tests;

[CollectionDefinition(Name)]
public sealed class AppHostTestCollection : ICollectionFixture<AppHostFixture>
{
    public const string Name = "app-host";
}
