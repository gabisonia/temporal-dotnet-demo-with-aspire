using Temporalio.Workflows;

namespace TemporalDemo.Payments.Api.Temporal;

[Workflow]
public interface IPaymentsHelloWorldWorkflow
{
    [WorkflowRun]
    Task RunAsync();
}

[Workflow]
public class PaymentsHelloWorldWorkflow : IPaymentsHelloWorldWorkflow
{
    [WorkflowRun]
    public async Task RunAsync()
    {
        await Workflow.ExecuteActivityAsync(
            PaymentActivityNames.PrintHelloWorld,
            Array.Empty<object>(),
            new ActivityOptions
            {
                StartToCloseTimeout = TimeSpan.FromSeconds(10),
            });
    }
}
