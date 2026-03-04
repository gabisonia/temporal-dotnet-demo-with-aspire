using Temporalio.Client;
using Temporalio.Exceptions;

namespace TemporalDemo.Payments.Api.Temporal;

public sealed class PaymentsCronStarterService(
    TemporalClient temporalClient,
    ILogger<PaymentsCronStarterService> logger) : IHostedService
{
    private const string HelloWorldCronExpression = "*/1 * * * *";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await temporalClient.StartWorkflowAsync(
                (IPaymentsHelloWorldWorkflow wf) => wf.RunAsync(),
                new WorkflowOptions(
                    id: PaymentsWorkflowIds.HelloWorldCron,
                    taskQueue: TemporalTaskQueues.Payments)
                {
                    CronSchedule = HelloWorldCronExpression,
                });

            logger.LogInformation(
                "Started payments demo cron workflow {WorkflowId} with cron {CronExpression}",
                PaymentsWorkflowIds.HelloWorldCron,
                HelloWorldCronExpression);
        }
        catch (WorkflowAlreadyStartedException)
        {
            logger.LogInformation(
                "Payments demo cron workflow {WorkflowId} is already running",
                PaymentsWorkflowIds.HelloWorldCron);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
