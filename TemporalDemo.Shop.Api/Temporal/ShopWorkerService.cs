using Temporalio.Client;
using Temporalio.Worker;

namespace TemporalDemo.Shop.Api.Temporal;

public sealed class ShopWorkerService : BackgroundService
{
    private readonly TemporalClient temporalClient;
    private readonly ShopActivities activities;
    private readonly ILogger<ShopWorkerService> logger;

    public ShopWorkerService(
        TemporalClient temporalClient,
        ShopActivities activities,
        ILogger<ShopWorkerService> logger)
    {
        this.temporalClient = temporalClient;
        this.activities = activities;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var worker = new TemporalWorker(
            temporalClient,
            new TemporalWorkerOptions(TemporalTaskQueues.Shop)
                .AddWorkflow<OrderWorkflow>()
                .AddActivity(activities.ReserveInventoryAsync)
                .AddActivity(activities.MarkOrderCompletedAsync)
                .AddActivity(activities.MarkOrderFailedAsync));

        logger.LogInformation("Shop Temporal worker started on task queue {TaskQueue}", TemporalTaskQueues.Shop);

        await worker.ExecuteAsync(stoppingToken);
    }
}
