using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace TemporalDemo.Shop.Api.Observability;

public sealed class ShopMetrics
{
    public const string MeterName = "TemporalDemo.Shop";

    private readonly Counter<long> _ordersCreated;
    private readonly Counter<long> _orderStatusTransitions;
    private readonly Histogram<double> _orderAmount;

    public ShopMetrics()
    {
        var meter = new Meter(MeterName);
        _ordersCreated = meter.CreateCounter<long>(
            "shop.orders.created",
            unit: "{order}",
            description: "Number of orders accepted by the shop service.");
        _orderStatusTransitions = meter.CreateCounter<long>(
            "shop.orders.status",
            unit: "{transition}",
            description: "Number of order status transitions.");
        _orderAmount = meter.CreateHistogram<double>(
            "shop.orders.amount",
            unit: "{currency}",
            description: "Order amount observed when orders are created.");
    }

    public void RecordOrderCreated(decimal amount)
    {
        _ordersCreated.Add(1);
        _orderAmount.Record(decimal.ToDouble(amount));
    }

    public void RecordOrderStatus(string status) =>
        _orderStatusTransitions.Add(1, new TagList
        {
            { "status", status }
        });
}