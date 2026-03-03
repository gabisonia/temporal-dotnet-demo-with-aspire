var builder = DistributedApplication.CreateBuilder(args);

var temporalPostgres = builder
    .AddContainer("temporal-postgres", "postgres", "16")
    .WithEnvironment("POSTGRES_USER", "temporal")
    .WithEnvironment("POSTGRES_PASSWORD", "temporal")
    .WithEnvironment("POSTGRES_DB", "temporal");

var temporal = builder
    .AddContainer("temporal", "temporalio/auto-setup", "1.26.2")
    .WithEnvironment("DB", "postgres12")
    .WithEnvironment("DB_PORT", "5432")
    .WithEnvironment("POSTGRES_USER", "temporal")
    .WithEnvironment("POSTGRES_PWD", "temporal")
    .WithEnvironment("POSTGRES_SEEDS", "temporal-postgres")
    .WaitFor(temporalPostgres)
    .WithEndpoint(name: "grpc", port: 7233, targetPort: 7233)
    .WithEndpoint(name: "ui", port: 8233, targetPort: 8233);

builder.AddProject("shop-api", "../TemporalDemo.Shop.Api/TemporalDemo.Shop.Api.csproj")
    .WaitFor(temporal)
    .WithEnvironment("Temporal__Address", "localhost:7233")
    .WithEnvironment("Temporal__Namespace", "default");

builder.AddProject("payments-api", "../TemporalDemo.Payments.Api/TemporalDemo.Payments.Api.csproj")
    .WaitFor(temporal)
    .WithEnvironment("Temporal__Address", "localhost:7233")
    .WithEnvironment("Temporal__Namespace", "default");

builder.Build().Run();
