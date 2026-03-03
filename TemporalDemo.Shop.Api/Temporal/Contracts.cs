using Temporalio.Activities;
using Temporalio.Workflows;

namespace TemporalDemo.Shop.Api.Temporal;

public static class TemporalTaskQueues
{
    public const string Shop = "shop-tq";
    public const string Payments = "payments-tq";
}

public static class PaymentActivityNames
{
    public const string ChargePayment = "ChargePayment";
}

public sealed record CreateOrderRequest(Guid Id, int Quantity);

public sealed record OrderWorkflowInput(string OrderId, Guid ProductId, int Quantity, decimal Amount);

public sealed record OrderWorkflowResult(string OrderId, string Status, string Message);

[Workflow]
public interface IOrderWorkflow
{
    [WorkflowRun]
    Task<OrderWorkflowResult> RunAsync(OrderWorkflowInput input);
}

public interface IShopActivities
{
    [Activity]
    Task ReserveInventoryAsync(OrderWorkflowInput input);

    [Activity]
    Task MarkOrderCompletedAsync(string orderId);

    [Activity]
    Task MarkOrderFailedAsync(string orderId, string reason);
}