using Temporalio.Client;
using TemporalDemo.Shop.Api.Infrastructure;
using TemporalDemo.Shop.Api.Temporal;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ShopStore>();
builder.Services.AddSingleton<ShopActivities>();
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();

app.MapGet("/products", (ShopStore store) => Results.Ok(store.GetProducts()));

app.MapGet("/orders/{orderId}", (string orderId, ShopStore store) =>
{
    var order = store.GetOrder(orderId);
    return order is null ? Results.NotFound() : Results.Ok(order);
});

app.MapPost("/orders", async (CreateOrderRequest request, ShopStore store, TemporalClient temporalClient) =>
{
    if (request.Quantity <= 0)
    {
        return Results.BadRequest("Quantity must be greater than zero.");
    }

    if (!store.TryGetProduct(request.Id, out var product) || product is null)
    {
        return Results.BadRequest($"Unknown product id '{request.Id}'.");
    }

    var orderId = Guid.NewGuid().ToString("N");
    var amount = request.Quantity * product.Price;
    var workflowInput = new OrderWorkflowInput(orderId, request.Id, request.Quantity, amount);

    store.CreatePendingOrder(workflowInput);

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