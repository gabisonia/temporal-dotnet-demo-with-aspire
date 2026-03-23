using TemporalDemo.AppHost;

var builder = DistributedApplication.CreateBuilder(args);
var settings = AppHostSettings.Load(builder.Configuration);

// Infrastructure resources
var temporalPostgres = builder.AddPostgres("temporal-postgres");
var temporalDatabase = temporalPostgres.AddDatabase("temporal-db", "temporal");

var temporalServer = builder
    .AddContainer("temporal", "temporalio/auto-setup", "1.26.2")
    .WithEnvironment("DB", settings.Temporal.Server.Db)
    .WithEnvironment("DB_PORT", temporalPostgres.Resource.Port)
    .WithEnvironment("POSTGRES_USER", temporalPostgres.Resource.UserNameReference)
    .WithEnvironment("POSTGRES_PWD", temporalPostgres.Resource.PasswordParameter)
    .WithEnvironment("POSTGRES_SEEDS", temporalPostgres.Resource.Host)
    .WaitFor(temporalDatabase)
    .WithEndpoint(name: "grpc", targetPort: 7233);

var temporalGrpcEndpoint = temporalServer.GetEndpoint("grpc");

builder.AddContainer("temporal-ui", "temporalio/ui")
    .WithEnvironment("TEMPORAL_ADDRESS", temporalGrpcEndpoint.Property(EndpointProperty.HostAndPort))
    .WaitFor(temporalServer)
    .WithHttpEndpoint(name: "http", targetPort: 8080);

var appDatabaseServer = builder.AddPostgres("app-db");
var appDatabase = appDatabaseServer.AddDatabase("AppDb", settings.AppDatabase.Database);

// Application resources
var paymentsApi = builder.AddProject<Projects.TemporalDemo_Payments_Api>("payments-api")
    .WaitFor(temporalServer)
    .WaitFor(appDatabase)
    .WithReference(appDatabase)
    .WithEnvironment("Temporal__Address", temporalGrpcEndpoint.Property(EndpointProperty.HostAndPort))
    .WithEnvironment("Temporal__Namespace", settings.Temporal.Client.Namespace);

var shopApi = builder.AddProject<Projects.TemporalDemo_Shop_Api>("shop-api")
    .WaitFor(temporalServer)
    .WaitFor(appDatabase)
    .WithReference(appDatabase)
    .WithEnvironment("Temporal__Address", temporalGrpcEndpoint.Property(EndpointProperty.HostAndPort))
    .WithEnvironment("Temporal__Namespace", settings.Temporal.Client.Namespace);

// From shop service we call payments api
shopApi
    .WithReference(paymentsApi)
    .WaitFor(paymentsApi);

builder.Build().Run();