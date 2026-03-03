using TemporalDemo.Payments.Api.Infrastructure;
using Temporalio.Activities;

namespace TemporalDemo.Payments.Api.Temporal;

public sealed class PaymentsActivities
{
    private readonly PaymentsStore store;

    public PaymentsActivities(PaymentsStore store)
    {
        this.store = store;
    }

    [Activity(PaymentActivityNames.ChargePayment)]
    public Task ChargePaymentAsync(string orderId, decimal amount)
    {
        store.Charge(orderId, amount);
        return Task.CompletedTask;
    }
}