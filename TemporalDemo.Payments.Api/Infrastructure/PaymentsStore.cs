using System.Collections.Concurrent;

namespace TemporalDemo.Payments.Api.Infrastructure;

public sealed class PaymentsStore
{
    private readonly ConcurrentDictionary<string, PaymentRecord> payments = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<PaymentRecord> GetAll() => payments.Values.OrderBy(x => x.UpdatedAtUtc).ToArray();

    public PaymentRecord? Get(string orderId)
    {
        payments.TryGetValue(orderId, out var record);
        return record;
    }

    public PaymentRecord Charge(string orderId, decimal amount)
    {
        // Deterministic demo decline rule.
        if (amount > 5_000m)
        {
            var failed = new PaymentRecord(orderId, amount, "declined", DateTimeOffset.UtcNow);
            payments[orderId] = failed;
            throw new InvalidOperationException($"Payment declined for order '{orderId}' because amount exceeds limit.");
        }

        var approved = new PaymentRecord(orderId, amount, "approved", DateTimeOffset.UtcNow);
        payments[orderId] = approved;
        return approved;
    }
}

public sealed record PaymentRecord(string OrderId, decimal Amount, string Status, DateTimeOffset UpdatedAtUtc);
