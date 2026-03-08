using Temporalio.Client;
using Temporalio.Exceptions;

namespace TemporalDemo.Payments.Api.Temporal;

public sealed class PaymentsCronStarterService(
    TemporalClient temporalClient,
    ILogger<PaymentsCronStarterService> logger) : IHostedService
{
    private const string HelloWorldCronExpression = "*/1 * * * *";
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(30);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

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
                return;
            }
            catch (WorkflowAlreadyStartedException)
            {
                logger.LogInformation(
                    "Payments demo cron workflow {WorkflowId} is already running",
                    PaymentsWorkflowIds.HelloWorldCron);
                return;
            }
            catch (RpcException ex) when (IsNamespaceNotReady(ex))
            {
                if (DateTimeOffset.UtcNow - startedAt >= StartupTimeout)
                {
                    throw;
                }

                logger.LogInformation(
                    "Temporal namespace is not ready for payments cron starter yet, retrying");

                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static bool IsNamespaceNotReady(RpcException exception) =>
        exception.Code == RpcException.StatusCode.NotFound
        && exception.Message.Contains("Namespace default is not found", StringComparison.OrdinalIgnoreCase);
}
