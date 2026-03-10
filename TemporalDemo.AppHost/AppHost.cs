using Aspire.Hosting.ApplicationModel;
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

var temporalGrpcEndpoint = temporal.GetEndpoint("grpc");

var temporalUi = builder.AddContainer("temporal-ui", "temporalio/ui")
    .WithEnvironment("TEMPORAL_ADDRESS", temporalGrpcEndpoint.Property(EndpointProperty.HostAndPort))
    .WaitFor(temporal)
    .WithHttpEndpoint(name: "http", port: settings.Temporal.Ui.Port, targetPort: 8080);

var appDatabaseUser = builder.AddParameter(
    "app-db-user",
    settings.AppDatabase.User,
    publishValueAsDefault: true,
    secret: false);
var appDatabasePassword = builder.AddParameter(
    "app-db-password",
    settings.AppDatabase.Password,
    publishValueAsDefault: false,
    secret: true);
var appDatabaseServer = builder.AddPostgres("app-db", appDatabaseUser, appDatabasePassword, settings.AppDatabase.Port);
var appDatabase = appDatabaseServer.AddDatabase("AppDb", settings.AppDatabase.Database);

var shopApi = builder.AddProject<Projects.TemporalDemo_Shop_Api>("shop-api")
    .WaitFor(temporal)
    .WaitFor(appDatabase)
    .WithReference(appDatabase)
    .WithEnvironment("Temporal__Address", temporalGrpcEndpoint.Property(EndpointProperty.HostAndPort))
    .WithEnvironment("Temporal__Namespace", settings.Temporal.Client.Namespace);

var paymentsApi = builder.AddProject<Projects.TemporalDemo_Payments_Api>("payments-api")
    .WaitFor(temporal)
    .WaitFor(appDatabase)
    .WithReference(appDatabase)
    .WithEnvironment("Temporal__Address", temporalGrpcEndpoint.Property(EndpointProperty.HostAndPort))
    .WithEnvironment("Temporal__Namespace", settings.Temporal.Client.Namespace);

shopApi
    .WithReference(paymentsApi)
    .WaitFor(paymentsApi);

builder.Build().Run();