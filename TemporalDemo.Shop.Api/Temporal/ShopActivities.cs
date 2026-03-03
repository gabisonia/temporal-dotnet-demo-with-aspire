using TemporalDemo.Shop.Api.Infrastructure;
using Temporalio.Activities;

namespace TemporalDemo.Shop.Api.Temporal;

public sealed class ShopActivities(ShopStore store) : IShopActivities
{
    [Activity]
    public Task ReserveInventoryAsync(OrderWorkflowInput input)
    {
        store.ReserveInventory(input);
        return Task.CompletedTask;
    }

    [Activity]
    public Task MarkOrderCompletedAsync(string orderId)
    {
        store.MarkCompleted(orderId);
        return Task.CompletedTask;
    }

    [Activity]
    public Task MarkOrderFailedAsync(string orderId, string reason)
    {
        store.MarkFailed(orderId, reason);
        return Task.CompletedTask;
    }
}