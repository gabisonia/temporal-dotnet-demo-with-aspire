using TemporalDemo.Payments.Api.Infrastructure;
using Temporalio.Activities;

namespace TemporalDemo.Payments.Api.Temporal;

public sealed class PaymentsActivities(PaymentsStore store)
{
    [Activity(PaymentActivityNames.ChargePayment)]
    public Task ChargePaymentAsync(string orderId, decimal amount)
    {
        store.Charge(orderId, amount);
        return Task.CompletedTask;
    }
}