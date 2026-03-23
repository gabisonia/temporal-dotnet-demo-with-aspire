using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using Temporalio.Client;
using TemporalDemo.Payments.Api.Infrastructure;
using TemporalDemo.Payments.Api.Observability;
using TemporalDemo.Payments.Api.Temporal;
using TemporalDemo.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddMeter(PaymentsMetrics.MeterName));

var appDbConnectionString = builder.Configuration.GetConnectionString("AppDb")
                            ?? throw new InvalidOperationException("Connection string 'AppDb' is not configured.");

builder.Services.AddDbContextFactory<PaymentsDbContext>(options =>
    options.UseNpgsql(appDbConnectionString));
builder.Services.AddSingleton<PaymentsMetrics>();
builder.Services.AddSingleton<PaymentsStore>();
builder.Services.AddSingleton<PaymentsActivities>();
builder.Services.AddSingleton<PaymentsDatabaseInitializer>();
builder.Services.AddSingleton<TemporalClient>(_ =>
{
    var address = builder.Configuration["Temporal:Address"] ?? "localhost:7233";
    var temporalNamespace = builder.Configuration["Temporal:Namespace"] ?? "default";

    return TemporalClient.ConnectAsync(new(address)
    {
        Namespace = temporalNamespace,
    }).GetAwaiter().GetResult();
});
builder.Services.AddHostedService<PaymentsWorkerService>();
builder.Services.AddHostedService<PaymentsCronStarterService>();

var app = builder.Build();

await app.Services.GetRequiredService<PaymentsDatabaseInitializer>().InitializeAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();

app.MapGet("/payments", async (PaymentsStore store, CancellationToken cancellationToken) =>
    Results.Ok(await store.GetAllAsync(cancellationToken)));

app.MapGet("/payments/{orderId}", async (string orderId, PaymentsStore store, CancellationToken cancellationToken) =>
{
    var payment = await store.GetAsync(orderId, cancellationToken);
    return payment is null ? Results.NotFound() : Results.Ok(payment);
});

app.Run();
