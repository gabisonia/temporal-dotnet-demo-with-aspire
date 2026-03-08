using TemporalDemo.Payments.Api.Infrastructure;
using Temporalio.Activities;

namespace TemporalDemo.Payments.Api.Temporal;

public sealed class PaymentsActivities(
    PaymentsStore store,
    ILogger<PaymentsActivities> logger)
{
    [Activity(PaymentActivityNames.ChargePayment)]
    public Task ChargePaymentAsync(string orderId, decimal amount) =>
        store.ChargeAsync(orderId, amount);

    [Activity(PaymentActivityNames.PrintHelloWorld)]
    public Task PrintHelloWorldAsync()
    {
        logger.LogInformation("hello world");
        Console.WriteLine("hello world");
        return Task.CompletedTask;
    }
}
