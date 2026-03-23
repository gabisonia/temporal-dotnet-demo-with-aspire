using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace TemporalDemo.Payments.Api.Observability;

public sealed class PaymentsMetrics
{
    public const string MeterName = "TemporalDemo.Payments";

    private readonly Counter<long> _chargeAttempts;
    private readonly Counter<long> _chargeResults;
    private readonly Histogram<double> _chargeAmount;

    public PaymentsMetrics()
    {
        var meter = new Meter(MeterName);
        _chargeAttempts = meter.CreateCounter<long>(
            "payments.charge.attempts",
            unit: "{attempt}",
            description: "Number of payment charge attempts.");
        _chargeResults = meter.CreateCounter<long>(
            "payments.charge.results",
            unit: "{charge}",
            description: "Number of payment charge results by status.");
        _chargeAmount = meter.CreateHistogram<double>(
            "payments.charge.amount",
            unit: "{currency}",
            description: "Payment amount observed during charge attempts.");
    }

    public void RecordChargeAttempt(decimal amount)
    {
        _chargeAttempts.Add(1);
        _chargeAmount.Record(decimal.ToDouble(amount));
    }

    public void RecordChargeResult(string status, decimal amount) =>
        _chargeResults.Add(1, new TagList
        {
            { "status", status },
            { "amount_range", amount > 5_000m ? "high" : "normal" }
        });
}