# Temporal Demo (.NET 10, Aspire, Shop + Payments)

Sample distributed demo with:

- `TemporalDemo.Shop.Api`: API + Temporal worker (workflow + shop activities)
- `TemporalDemo.Payments.Api`: API + Temporal worker (payment activity)
- `TemporalDemo.AppHost`: .NET Aspire orchestrator
- `TemporalDemo.ServiceDefaults`: shared Aspire defaults

This demo uses PostgreSQL-backed persistence with EF Core.

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
- Temporal UI is exposed at `http://localhost:8233`.
- Open each API Swagger UI from the dashboard service links (or `/swagger` on each API URL).

## Generate Docker Compose

This AppHost is configured to publish Docker Compose artifacts.

```bash
aspire publish -o aspire-output
```

Generated files are written under `aspire-output/`, including `docker-compose.yaml` and environment files for the published app.

Start the published stack with:

```bash
docker compose -f aspire-output/docker-compose.yaml up -d
```

## Architecture

The Aspire host starts four infrastructure containers/resources:

- `temporal-postgres`: Postgres used only by Temporal
- `temporal`: Temporal server
- `temporal-ui`: Temporal UI
- `app-db`: shared application Postgres instance for shop and payments

The application database is shared by both services, but each service writes to its own schema:

- `shop-api` uses schema `shop`
- `payments-api` uses schema `payments`

There is one physical Postgres database for business data:

- Database: `temporaldemoapp`
- Port: `5434`

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

- `ConnectionStrings:AppDb`
- `AppDatabase:*`
- `Temporal:Postgres:*`
- `Temporal:Server:*`
- `Temporal:Ui:*`
- `Temporal:Client:*`

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

At runtime, AppHost also passes the same connection string to both services through environment variables.

### Temporal configuration

Both APIs also receive:

- `Temporal:Address`
- `Temporal:Namespace`

These are currently provided by AppHost and default to:

- address: `localhost:7233`
- namespace: `default`

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
- Shop and payments share one business Postgres instance, but use different schemas.
- No centralized contracts project; service-local contracts are used.
