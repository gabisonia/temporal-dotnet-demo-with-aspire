using Temporalio.Client;
using TemporalDemo.Payments.Api.Infrastructure;
using TemporalDemo.Payments.Api.Temporal;
using TemporalDemo.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<PaymentsStore>();
builder.Services.AddSingleton<PaymentsActivities>();
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();

app.MapGet("/payments", (PaymentsStore store) => Results.Ok(store.GetAll()));

app.MapGet("/payments/{orderId}", (string orderId, PaymentsStore store) =>
{
    var payment = store.Get(orderId);
    return payment is null ? Results.NotFound() : Results.Ok(payment);
});

app.Run();
