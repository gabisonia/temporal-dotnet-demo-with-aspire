using Temporalio.Workflows;

namespace TemporalDemo.Shop.Api.Temporal;

[Workflow]
public class OrderWorkflow : IOrderWorkflow
{
    [WorkflowRun]
    public async Task<OrderWorkflowResult> RunAsync(OrderWorkflowInput input)
    {
        try
        {
            await Workflow.ExecuteActivityAsync(
                (IShopActivities act) => act.ReserveInventoryAsync(input),
                new ActivityOptions
                {
                    StartToCloseTimeout = TimeSpan.FromSeconds(15),
                });

            await Workflow.ExecuteActivityAsync(
                PaymentActivityNames.ChargePayment,
                [input.OrderId, input.Amount],
                new ActivityOptions
                {
                    StartToCloseTimeout = TimeSpan.FromSeconds(15),
                    TaskQueue = TemporalTaskQueues.Payments,
                });

            await Workflow.ExecuteActivityAsync(
                (IShopActivities act) => act.MarkOrderCompletedAsync(input.OrderId),
                new ActivityOptions
                {
                    StartToCloseTimeout = TimeSpan.FromSeconds(10),
                });

            return new OrderWorkflowResult(input.OrderId, "completed", "Order completed successfully.");
        }
        catch (Exception ex)
        {
            await Workflow.ExecuteActivityAsync(
                (IShopActivities act) => act.MarkOrderFailedAsync(input.OrderId, ex.Message),
                new ActivityOptions
                {
                    StartToCloseTimeout = TimeSpan.FromSeconds(10),
                });

            return new OrderWorkflowResult(input.OrderId, "failed", ex.Message);
        }
    }
}
