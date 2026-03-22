using System.Text.Json;

namespace TemporalDemo.AppHost.Tests;

[Collection(AppHostTestCollection.Name)]
public sealed class DistributedApplicationTests(AppHostFixture fixture)
{
    [Fact]
    public async Task ShopApiProductsEndpointReturnsSeededProducts()
    {
        await fixture.WaitForResourceHealthyAsync("shop-api");

        using var httpClient = fixture.CreateHttpClient("shop-api");
        using var response = await httpClient.GetAsync("/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await AppHostFixture.ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, payload.ValueKind);
        Assert.True(payload.GetArrayLength() >= 3);
        Assert.Contains(payload.EnumerateArray(), product =>
            product.GetProperty("name").GetString() is "Laptop");
    }

    [Fact]
    public async Task PaymentsApiEndpointReturnsAJsonArray()
    {
        await fixture.WaitForResourceHealthyAsync("payments-api");

        using var httpClient = fixture.CreateHttpClient("payments-api");
        using var response = await httpClient.GetAsync("/payments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await AppHostFixture.ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, payload.ValueKind);
    }

    [Fact]
    public async Task ShopApiCanReachPaymentsApiThroughServiceDiscovery()
    {
        await fixture.WaitForResourceHealthyAsync("shop-api");
        await fixture.WaitForResourceHealthyAsync("payments-api");

        using var httpClient = fixture.CreateHttpClient("shop-api");
        using var response = await httpClient.GetAsync("/demo/service-discovery/payments/missing-order");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var payload = await AppHostFixture.ReadJsonAsync(response);
        Assert.Equal("payments-api", payload.GetProperty("service").GetString());
        Assert.Equal("https+http://payments-api", payload.GetProperty("baseAddress").GetString());
        Assert.Equal("missing-order", payload.GetProperty("orderId").GetString());
    }
}
