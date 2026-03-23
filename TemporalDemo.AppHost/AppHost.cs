using TemporalDemo.AppHost;

var builder = DistributedApplication.CreateBuilder(args);
var settings = AppHostSettings.Load(builder.Configuration);

// Parameters
var temporalServerDb = builder.AddParameter("temporal-server-db", settings.Temporal.Server.Db);
var temporalNamespace = builder.AddParameter("temporal-namespace", settings.Temporal.Client.Namespace);
var temporalPostgresUser = builder.AddParameter("temporal-postgres-user", "postgres");
var appDbPostgresUser = builder.AddParameter("app-db-postgres-user", "postgres");

// Infrastructure resources
var temporalPostgres = builder.AddPostgres(
    "temporal-postgres",
    userName: temporalPostgresUser);
var temporalDatabase = temporalPostgres.AddDatabase("temporal-db", "temporal");

var temporalServer = builder
    .AddContainer("temporal", "temporalio/auto-setup", "1.26.2")
    .WithEnvironment("DB", temporalServerDb)
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

var appDatabaseServer = builder.AddPostgres(
    "app-db",
    userName: appDbPostgresUser);
var appDatabase = appDatabaseServer.AddDatabase("AppDb", settings.AppDatabase.Database);

// Application resources
var paymentsApi = builder.AddProject<Projects.TemporalDemo_Payments_Api>("payments-api")
    .WaitFor(temporalServer)
    .WaitFor(appDatabase)
    .WithReference(appDatabase)
    .WithEnvironment("Temporal__Address", temporalGrpcEndpoint.Property(EndpointProperty.HostAndPort))
    .WithEnvironment("Temporal__Namespace", temporalNamespace);

var shopApi = builder.AddProject<Projects.TemporalDemo_Shop_Api>("shop-api")
    .WaitFor(temporalServer)
    .WaitFor(appDatabase)
    .WithReference(appDatabase)
    .WithEnvironment("Temporal__Address", temporalGrpcEndpoint.Property(EndpointProperty.HostAndPort))
    .WithEnvironment("Temporal__Namespace", temporalNamespace);

// From shop service we call payments api
shopApi
    .WithReference(paymentsApi)
    .WaitFor(paymentsApi);

builder.Build().Run();