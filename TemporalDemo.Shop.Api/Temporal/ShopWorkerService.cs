using Temporalio.Client;
using Temporalio.Worker;

namespace TemporalDemo.Shop.Api.Temporal;

public sealed class ShopWorkerService(
    TemporalClient temporalClient,
    ShopActivities activities,
    ILogger<ShopWorkerService> logger)
    : BackgroundService
{
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