using Microsoft.EntityFrameworkCore;
using TemporalDemo.ServiceDefaults;
using Temporalio.Client;
using TemporalDemo.Shop.Api.Infrastructure;
using TemporalDemo.Shop.Api.Temporal;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var appDbConnectionString = builder.Configuration.GetConnectionString("AppDb")
                            ?? throw new InvalidOperationException("Connection string 'AppDb' is not configured.");

builder.Services.AddDbContextFactory<ShopDbContext>(options =>
    options.UseNpgsql(appDbConnectionString));
builder.Services.AddSingleton<ShopStore>();
builder.Services.AddSingleton<ShopActivities>();
builder.Services.AddSingleton<ShopDatabaseInitializer>();
builder.Services.AddSingleton<TemporalClient>(_ =>
{
    var address = builder.Configuration["Temporal:Address"] ?? "localhost:7233";
    var temporalNamespace = builder.Configuration["Temporal:Namespace"] ?? "default";

    return TemporalClient.ConnectAsync(new(address)
    {
        Namespace = temporalNamespace,
    }).GetAwaiter().GetResult();
});
builder.Services.AddHostedService<ShopWorkerService>();

var app = builder.Build();

await app.Services.GetRequiredService<ShopDatabaseInitializer>().InitializeAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();

app.MapGet("/products", async (ShopStore store, CancellationToken cancellationToken) =>
    Results.Ok(await store.GetProductsAsync(cancellationToken)));

app.MapGet("/orders/{orderId}", async (string orderId, ShopStore store, CancellationToken cancellationToken) =>
{
    var order = await store.GetOrderAsync(orderId, cancellationToken);
    return order is null ? Results.NotFound() : Results.Ok(order);
});

app.MapPost("/orders", async (
    CreateOrderRequest request,
    ShopStore store,
    TemporalClient temporalClient,
    CancellationToken cancellationToken) =>
{
    if (request.Quantity <= 0)
    {
        return Results.BadRequest("Quantity must be greater than zero.");
    }

    var product = await store.GetProductAsync(request.Id, cancellationToken);
    if (product is null)
    {
        return Results.BadRequest($"Unknown product id '{request.Id}'.");
    }

    var orderId = Guid.NewGuid().ToString("N");
    var amount = request.Quantity * product.Price;
    var workflowInput = new OrderWorkflowInput(orderId, request.Id, request.Quantity, amount);

    await store.CreatePendingOrderAsync(workflowInput, cancellationToken);

    await temporalClient.StartWorkflowAsync(
        (IOrderWorkflow wf) => wf.RunAsync(workflowInput),
        new(id: $"order-{orderId}", taskQueue: TemporalTaskQueues.Shop));

    return Results.Accepted($"/orders/{orderId}", new
    {
        orderId,
        status = "started",
        amount,
    });
});

app.Run();