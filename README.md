# Temporal Demo (.NET 10, Aspire, Shop + Payments)

Sample distributed demo with:

- `TemporalDemo.Shop.Api`: API + Temporal worker (workflow + shop activities)
- `TemporalDemo.Payments.Api`: API + Temporal worker (payment activity)
- `TemporalDemo.AppHost`: .NET Aspire orchestrator
- `TemporalDemo.ServiceDefaults`: shared Aspire defaults

This demo uses in-memory storage only.

## Requirements

- .NET SDK `10.0.100+`
- Docker (for Temporal container started by Aspire)

## Run

```bash
dotnet restore TemporalDemo.slnx
dotnet run --project TemporalDemo.AppHost
```

After startup:

- Aspire dashboard URL is printed in terminal output.
- Temporal UI is exposed at `http://localhost:8233`.
- Open each API Swagger UI from the dashboard service links (or `/swagger` on each API URL).

## Demo flow

1. Create an order via Shop API:

```http
POST /orders
Content-Type: application/json

{
  "id": "11111111-1111-1111-1111-111111111111",
  "quantity": 1
}
```

2. Check order state:

```http
GET /orders/{orderId}
```

3. Check payment state:

```http
GET /payments/{orderId}
```

## What to call to test

1. Start the app:

```bash
dotnet run --project TemporalDemo.AppHost
```

2. Open Aspire dashboard and copy service URLs:
- `shop-api` base URL (example: `http://localhost:52xx`)
- `payments-api` base URL (example: `http://localhost:53xx`)

3. Get products (contains `id`, `name`, `price`):

```bash
curl -sS "$SHOP_URL/products"
```

4. Create a successful order (amount <= 5000):

```bash
curl -sS -X POST "$SHOP_URL/orders" \
  -H "Content-Type: application/json" \
  -d '{"id":"11111111-1111-1111-1111-111111111111","quantity":1}'
```

Expected: response contains `orderId` and `status: "started"`.

5. Check order status:

```bash
curl -sS "$SHOP_URL/orders/<ORDER_ID>"
```

Expected after a few seconds: order `status` becomes `completed`.

6. Check payment status:

```bash
curl -sS "$PAYMENTS_URL/payments/<ORDER_ID>"
```

Expected: payment `status` is `approved`.

7. Test failure path (payment decline, amount > 5000):

```bash
curl -sS -X POST "$SHOP_URL/orders" \
  -H "Content-Type: application/json" \
  -d '{"id":"11111111-1111-1111-1111-111111111111","quantity":5}'
```

Then check:

```bash
curl -sS "$SHOP_URL/orders/<ORDER_ID>"
curl -sS "$PAYMENTS_URL/payments/<ORDER_ID>"
```

Expected: payment `status` is `declined` and order `status` is `failed`.

## Behavior notes

- Shop starts the workflow on task queue `shop-tq`.
- Workflow calls payment activity on task queue `payments-tq`.
- Payments service declines charges above `5000`.
- No centralized contracts project; service-local contracts are used.
