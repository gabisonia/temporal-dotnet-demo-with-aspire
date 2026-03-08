using TemporalDemo.Shop.Api.Infrastructure;
using Temporalio.Activities;

namespace TemporalDemo.Shop.Api.Temporal;

public sealed class ShopActivities(ShopStore store) : IShopActivities
{
    [Activity]
    public Task ReserveInventoryAsync(OrderWorkflowInput input) =>
        store.ReserveInventoryAsync(input);

    [Activity]
    public Task MarkOrderCompletedAsync(string orderId) =>
        store.MarkCompletedAsync(orderId);

    [Activity]
    public Task MarkOrderFailedAsync(string orderId, string reason) =>
        store.MarkFailedAsync(orderId, reason);
}