using Temporalio.Client;
using Temporalio.Worker;

namespace TemporalDemo.Payments.Api.Temporal;

public sealed class PaymentsWorkerService(
    TemporalClient temporalClient,
    PaymentsActivities activities,
    ILogger<PaymentsWorkerService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var worker = new TemporalWorker(
            temporalClient,
            new TemporalWorkerOptions(TemporalTaskQueues.Payments)
                .AddWorkflow<PaymentsHelloWorldWorkflow>()
                .AddActivity(activities.ChargePaymentAsync)
                .AddActivity(activities.PrintHelloWorldAsync));

        logger.LogInformation("Payments Temporal worker started on task queue {TaskQueue}",
            TemporalTaskQueues.Payments);

        await worker.ExecuteAsync(stoppingToken);
    }
}
