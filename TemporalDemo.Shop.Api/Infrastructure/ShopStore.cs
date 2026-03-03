using System.Collections.Concurrent;
using TemporalDemo.Shop.Api.Temporal;

namespace TemporalDemo.Shop.Api.Infrastructure;

public sealed class ShopStore
{
    private readonly ConcurrentDictionary<Guid, ProductInventory> products = new()
    {
        [Guid.Parse("11111111-1111-1111-1111-111111111111")] = new("Laptop", 1200m, 5),
        [Guid.Parse("22222222-2222-2222-2222-222222222222")] = new("Headphones", 250m, 12),
        [Guid.Parse("33333333-3333-3333-3333-333333333333")] = new("Mouse", 80m, 25),
    };

    private readonly ConcurrentDictionary<string, ShopOrder> orders = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<ShopProduct> GetProducts() =>
        products
            .Select(x => new ShopProduct(x.Key, x.Value.Name, x.Value.Price))
            .OrderBy(x => x.Name)
            .ToArray();

    public bool TryGetProduct(Guid id, out ShopProduct? product)
    {
        if (!products.TryGetValue(id, out var inventory))
        {
            product = null;
            return false;
        }

        product = new ShopProduct(id, inventory.Name, inventory.Price);
        return true;
    }

    public ShopOrder? GetOrder(string orderId)
    {
        orders.TryGetValue(orderId, out var order);
        return order;
    }

    public void CreatePendingOrder(OrderWorkflowInput input)
    {
        var order = new ShopOrder(input.OrderId, input.ProductId, input.Quantity, input.Amount, "pending", null);
        orders[input.OrderId] = order;
    }

    public void ReserveInventory(OrderWorkflowInput input)
    {
        if (!products.TryGetValue(input.ProductId, out var inStock))
        {
            throw new InvalidOperationException($"Product '{input.ProductId}' does not exist.");
        }

        if (inStock.Stock < input.Quantity)
        {
            throw new InvalidOperationException($"Not enough inventory for '{input.ProductId}'.");
        }

        products[input.ProductId] = inStock with { Stock = inStock.Stock - input.Quantity };

        orders.AddOrUpdate(
            input.OrderId,
            _ => new ShopOrder(input.OrderId, input.ProductId, input.Quantity, input.Amount, "inventory_reserved", null),
            (_, existing) => existing with { Status = "inventory_reserved", FailureReason = null });
    }

    public void MarkCompleted(string orderId)
    {
        if (!orders.TryGetValue(orderId, out var existing))
        {
            return;
        }

        orders[orderId] = existing with { Status = "completed", FailureReason = null };
    }

    public void MarkFailed(string orderId, string reason)
    {
        if (!orders.TryGetValue(orderId, out var existing))
        {
            return;
        }

        orders[orderId] = existing with { Status = "failed", FailureReason = reason };
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

internal sealed record ProductInventory(string Name, decimal Price, int Stock);
