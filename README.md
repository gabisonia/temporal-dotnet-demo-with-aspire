# Temporal Demo (.NET 10, Aspire, Shop + Payments)

Sample distributed demo with:

- `TemporalDemo.Shop.Api`: API + Temporal worker (workflow + shop activities)
- `TemporalDemo.Payments.Api`: API + Temporal worker (payment activity)
- `TemporalDemo.AppHost`: .NET Aspire orchestrator
- `TemporalDemo.ServiceDefaults`: shared Aspire defaults

This demo uses PostgreSQL-backed persistence with EF Core.
It also includes a simple Aspire service discovery sample where `shop-api` calls `payments-api` over HTTP using the service name `payments-api`.

## Requirements

- .NET SDK `10.0.100+`
- Docker

## Run

```bash
dotnet restore TemporalDemo.slnx
dotnet run --project TemporalDemo.AppHost
```

After startup:

- Aspire dashboard URL is printed in terminal output.
- Temporal UI is exposed through the dashboard service links on a dynamically assigned local port.
- Open each API Swagger UI from the dashboard service links (or `/swagger` on each API URL).

## Architecture

The Aspire host starts four infrastructure resources:

- `temporal-postgres`: Postgres used only by Temporal
- `temporal`: Temporal server
- `temporal-ui`: Temporal UI
- `app-db`: Aspire PostgreSQL server resource for shop and payments
- `AppDb`: Aspire PostgreSQL database resource referenced by both APIs

The application database is shared by both services, but each service writes to its own schema:

- `shop-api` uses schema `shop`
- `payments-api` uses schema `payments`

There is one physical Postgres database for business data:

- Database: `temporaldemoapp`

This means shop and payments share the same Postgres server and database, but their tables remain isolated by schema.

## Database usage

### Shop

`shop-api` persists:

- products
- orders

On startup, `ShopDatabaseInitializer`:

- creates the `shop` schema objects if they do not exist
- seeds three products if the products table is empty

Seeded product IDs:

- `11111111-1111-1111-1111-111111111111` = Laptop
- `22222222-2222-2222-2222-222222222222` = Headphones
- `33333333-3333-3333-3333-333333333333` = Mouse

### Payments

`payments-api` persists:

- payment records keyed by `orderId`

On startup, `PaymentsDatabaseInitializer` creates the `payments` schema objects if they do not exist.

### EF Core note

This project currently uses EF Core runtime schema creation via `Database.GenerateCreateScript()` and `ExecuteSqlRawAsync(...)`.
It does not use EF migrations yet.

## Configuration

### AppHost configuration

[TemporalDemo.AppHost/appsettings.json](/Users/gabisonia/Desktop/TemporalDemo/TemporalDemo.AppHost/appsettings.json) contains:

- `AppDatabase:Database`
- `Temporal:Server:Db`
- `Temporal:Client:Namespace`

[TemporalDemo.AppHost/AppHostSettings.cs](/Users/gabisonia/Desktop/TemporalDemo/TemporalDemo.AppHost/AppHostSettings.cs) binds and validates these settings so [TemporalDemo.AppHost/AppHost.cs](/Users/gabisonia/Desktop/TemporalDemo/TemporalDemo.AppHost/AppHost.cs) stays focused on resource wiring.

### API configuration

The two APIs read the business database connection from their own config files:

- [TemporalDemo.Shop.Api/appsettings.json](/Users/gabisonia/Desktop/TemporalDemo/TemporalDemo.Shop.Api/appsettings.json)
- [TemporalDemo.Payments.Api/appsettings.json](/Users/gabisonia/Desktop/TemporalDemo/TemporalDemo.Payments.Api/appsettings.json)

Both expect:

```json
"ConnectionStrings": {
  "AppDb": "Host=localhost;Port=5434;Database=temporaldemoapp;Username=postgres;Password=postgres"
}
```

At runtime under AppHost, both services receive `ConnectionStrings__AppDb` through Aspire's `.WithReference(AppDb)` resource injection instead of a manually composed connection string.
AppHost lets Aspire allocate the local host port dynamically.

### Temporal configuration

Both APIs also receive:

- `Temporal:Address`
- `Temporal:Namespace`

AppHost now binds the Temporal address from the `temporal` gRPC endpoint allocation instead of hardcoding `localhost:7233`. Aspire allocates the local host port dynamically, and the namespace still defaults to `default`.

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

4. Check the same payment through Aspire service discovery:

```http
GET /demo/service-discovery/payments/{orderId}
```

This endpoint lives in `shop-api` and calls `payments-api` through a typed `HttpClient` configured with:

```csharp
new Uri("https+http://payments-api")
```

The AppHost enables this by wiring:

```csharp
shopApi.WithReference(paymentsApi)
```

That gives `shop-api` the service discovery endpoint metadata for `payments-api`, and `AddServiceDefaults()` enables resolution on `HttpClient`.

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

7. Check the same payment through `shop-api` using Aspire service discovery:

```bash
curl -sS "$SHOP_URL/demo/service-discovery/payments/<ORDER_ID>"
```

Expected: response includes:

- `service: "payments-api"`
- `baseAddress: "https+http://payments-api"`
- `payment.status: "approved"`

8. Test failure path (payment decline, amount > 5000):

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
- Shop and payments share one business Postgres instance, but use different schemas.
- No centralized contracts project; service-local contracts are used.
