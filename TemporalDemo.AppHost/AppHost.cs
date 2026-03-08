using TemporalDemo.AppHost;

var builder = DistributedApplication.CreateBuilder(args);
var settings = AppHostSettings.Load(builder.Configuration);

var temporalPostgres = builder
    .AddContainer("temporal-postgres", "postgres", "16")
    .WithEnvironment("POSTGRES_USER", settings.Temporal.Postgres.User)
    .WithEnvironment("POSTGRES_PASSWORD", settings.Temporal.Postgres.Password)
    .WithEnvironment("POSTGRES_DB", settings.Temporal.Postgres.Database);

var temporal = builder
    .AddContainer("temporal", "temporalio/auto-setup", "1.26.2")
    .WithEnvironment("DB", settings.Temporal.Server.Db)
    .WithEnvironment("DB_PORT", settings.Temporal.Server.DbPort)
    .WithEnvironment("POSTGRES_USER", settings.Temporal.Postgres.User)
    .WithEnvironment("POSTGRES_PWD", settings.Temporal.Postgres.Password)
    .WithEnvironment("POSTGRES_SEEDS", settings.Temporal.Server.PostgresSeeds)
    .WaitFor(temporalPostgres)
    .WithEndpoint(name: "grpc", port: settings.Temporal.Server.GrpcPort, targetPort: 7233);

var temporalUi = builder.AddContainer("temporal-ui", "temporalio/ui")
    .WithEnvironment("TEMPORAL_ADDRESS", "temporal:7233")
    .WaitFor(temporal)
    .WithHttpEndpoint(name: "http", port: settings.Temporal.Ui.Port, targetPort: 8080);

var appDatabase = builder
    .AddContainer("app-db", "postgres", "16")
    .WithEnvironment("POSTGRES_USER", settings.AppDatabase.User)
    .WithEnvironment("POSTGRES_PASSWORD", settings.AppDatabase.Password)
    .WithEnvironment("POSTGRES_DB", settings.AppDatabase.Database)
    .WithEndpoint(name: "postgres", port: settings.AppDatabase.Port, targetPort: 5432);

builder.AddProject<Projects.TemporalDemo_Shop_Api>("shop-api")
    .WaitFor(temporal)
    .WaitFor(temporalUi)
    .WaitFor(appDatabase)
    .WithEnvironment("ConnectionStrings__AppDb", settings.AppDbConnectionString)
    .WithEnvironment("Temporal__Address", settings.Temporal.Client.Address)
    .WithEnvironment("Temporal__Namespace", settings.Temporal.Client.Namespace);

builder.AddProject<Projects.TemporalDemo_Payments_Api>("payments-api")
    .WaitFor(temporal)
    .WaitFor(temporalUi)
    .WaitFor(appDatabase)
    .WithEnvironment("ConnectionStrings__AppDb", settings.AppDbConnectionString)
    .WithEnvironment("Temporal__Address", settings.Temporal.Client.Address)
    .WithEnvironment("Temporal__Namespace", settings.Temporal.Client.Namespace);

builder.Build().Run();