using System.Net;
using System.Net.Http.Json;

namespace TemporalDemo.Shop.Api;

public sealed class PaymentsApiClient(HttpClient httpClient)
{
    public async Task<PaymentsApiResult> GetPaymentAsync(string orderId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"/payments/{orderId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return PaymentsApiResult.NotFound;
        }

        response.EnsureSuccessStatusCode();

        var payment = await response.Content.ReadFromJsonAsync<PaymentView>(cancellationToken);
        return payment is null
            ? throw new InvalidOperationException("Payments API returned an empty response body.")
            : new PaymentsApiResult(payment);
    }
}

public sealed record PaymentView(string OrderId, decimal Amount, string Status, DateTimeOffset UpdatedAtUtc);

public sealed record PaymentsApiResult(PaymentView? Payment)
{
    public static PaymentsApiResult NotFound { get; } = new((PaymentView?)null);

    public bool Found => Payment is not null;
}
