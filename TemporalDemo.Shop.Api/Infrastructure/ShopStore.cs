using Microsoft.EntityFrameworkCore;
using TemporalDemo.Shop.Api.Observability;
using TemporalDemo.Shop.Api.Temporal;

namespace TemporalDemo.Shop.Api.Infrastructure;

public sealed class ShopStore(
    IDbContextFactory<ShopDbContext> dbContextFactory,
    ShopMetrics metrics)
{
    public async Task<IReadOnlyCollection<ShopProduct>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Products
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new ShopProduct(x.Id, x.Name, x.Price))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ShopProduct?> GetProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Products
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ShopProduct(x.Id, x.Name, x.Price))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ShopOrder?> GetOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Orders
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .Select(x => new ShopOrder(x.OrderId, x.ProductId, x.Quantity, x.Amount, x.Status, x.FailureReason))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task CreatePendingOrderAsync(
        OrderWorkflowInput input,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        dbContext.Orders.Add(new ShopOrderEntity
        {
            OrderId = input.OrderId,
            ProductId = input.ProductId,
            Quantity = input.Quantity,
            Amount = input.Amount,
            Status = "pending",
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        metrics.RecordOrderCreated(input.Amount);
        metrics.RecordOrderStatus("pending");
    }

    public async Task ReserveInventoryAsync(
        OrderWorkflowInput input,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var product = await dbContext.Products.SingleOrDefaultAsync(x => x.Id == input.ProductId, cancellationToken);
        if (product is null)
        {
            throw new InvalidOperationException($"Product '{input.ProductId}' does not exist.");
        }

        if (product.Stock < input.Quantity)
        {
            throw new InvalidOperationException($"Not enough inventory for '{input.ProductId}'.");
        }

        product.Stock -= input.Quantity;

        var order = await dbContext.Orders.SingleOrDefaultAsync(x => x.OrderId == input.OrderId, cancellationToken);
        if (order is null)
        {
            dbContext.Orders.Add(new ShopOrderEntity
            {
                OrderId = input.OrderId,
                ProductId = input.ProductId,
                Quantity = input.Quantity,
                Amount = input.Amount,
                Status = "inventory_reserved",
            });
        }
        else
        {
            order.Status = "inventory_reserved";
            order.FailureReason = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        metrics.RecordOrderStatus("inventory_reserved");
    }

    public async Task MarkCompletedAsync(string orderId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var order = await dbContext.Orders.SingleOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        order.Status = "completed";
        order.FailureReason = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        metrics.RecordOrderStatus("completed");
    }

    public async Task MarkFailedAsync(
        string orderId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var order = await dbContext.Orders.SingleOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        order.Status = "failed";
        order.FailureReason = reason;
        await dbContext.SaveChangesAsync(cancellationToken);
        metrics.RecordOrderStatus("failed");
    }
}

public sealed record ShopOrder(
    string OrderId,
    Guid ProductId,
    int Quantity,
    decimal Amount,
    string Status,
    string? FailureReason);

public sealed record ShopProduct(Guid Id, string Name, decimal Price);
