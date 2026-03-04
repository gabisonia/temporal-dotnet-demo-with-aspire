namespace TemporalDemo.Payments.Api.Temporal;

public static class TemporalTaskQueues
{
    public const string Payments = "payments-tq";
}

public static class PaymentActivityNames
{
    public const string ChargePayment = "ChargePayment";
    public const string PrintHelloWorld = "PrintHelloWorld";
}

public static class PaymentsWorkflowIds
{
    public const string HelloWorldCron = "payments-demo-hello-world-cron";
}
