using Temporalio.Client;
using Temporalio.Worker;

namespace TemporalDemo.Payments.Api.Temporal;

public sealed class PaymentsWorkerService : BackgroundService
{
    private readonly TemporalClient temporalClient;
    private readonly PaymentsActivities activities;
    private readonly ILogger<PaymentsWorkerService> logger;

    public PaymentsWorkerService(
        TemporalClient temporalClient,
        PaymentsActivities activities,
        ILogger<PaymentsWorkerService> logger)
    {
        this.temporalClient = temporalClient;
        this.activities = activities;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var worker = new TemporalWorker(
            temporalClient,
            new TemporalWorkerOptions(TemporalTaskQueues.Payments)
                .AddActivity(activities.ChargePaymentAsync));

        logger.LogInformation("Payments Temporal worker started on task queue {TaskQueue}",
            TemporalTaskQueues.Payments);

        await worker.ExecuteAsync(stoppingToken);
    }
}