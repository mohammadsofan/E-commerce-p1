# E-Commerce Backend — Clean Architecture

This repository contains a production-ready e-commerce backend built with Clean Architecture.

## Tech Stack

- **.NET 8** Web API (minimal hosting model)
- **Clean Architecture** (Domain / Application / Infrastructure / Api)
- **EF Core 8** + SQL Server
- **CQRS** with MediatR (commands, queries, validators, handlers)
- **Identity**: JWT auth + refresh tokens, role-based authorization
- **Payments**: Stripe SDK integration (test-mode local simulation with dummy keys)
- **Observability**: Serilog, Health Checks, Prometheus metrics, OpenTelemetry tracing + correlation IDs
- **Notifications**: Email (SMTP), SMS (Twilio-style), Push (FCM-style) — all graceful no-ops when unconfigured
- **Testing**: 223 tests (Domain, Application, Integration, Architecture)

## Project Structure

```
src/
  Ecommerce.Domain/         Entities, value objects, domain events, exceptions
  Ecommerce.Application/    DTOs, interfaces, CQRS features, validators
  Ecommerce.Infrastructure/ EF Core, Identity, repositories, services
  Ecommerce.Api/            Controllers, auth, middleware, swagger, Program.cs
tests/
  Ecommerce.Domain.Tests/   Unit tests
  Ecommerce.Application.Tests/ Handler + service tests
  Ecommerce.IntegrationTests/ API + DB integration tests
  Ecommerce.Architecture.Tests/ Dependency/convention tests (NetArchTest)
```

## Run Locally

### Prerequisites

- .NET SDK 8.0
- SQL Server on `localhost` (Windows auth) — or a Docker instance (see below)

### 1. Start SQL Server (optional — if you don't have one)

```powershell
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Your_password123" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

If you use the Docker SQL Server with an `sa` login, change the connection string in `src/Ecommerce.Api/appsettings.Development.json`:

```json
"DefaultConnection": "Server=localhost;Database=EcommerceDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

Otherwise the default Windows-auth connection string works as-is with SQL Server Express / LocalDB.

### 2. Run the API

```powershell
dotnet run --project src/Ecommerce.Api
```

Or use your IDE (Visual Studio / Rider) — the launch profile starts Swagger in a browser.

- Swagger UI: `http://localhost:5000/swagger`
- HTTPS: `https://localhost:7001`

The database is created and seeded automatically on first startup (`EnsureCreatedAsync` + `DbSeeder`).

### 3. Verify

- **Health checks**: `http://localhost:5000/health`
- **Metrics** (Prometheus): `http://localhost:5000/metrics`
- **Products**: `GET http://localhost:5000/api/products`
- **Product search**: `GET http://localhost:5000/api/products/search?searchTerm=laptop`

## Configuration

All settings live in `src/Ecommerce.Api/appsettings.Development.json`.

| Setting | Purpose | Local default |
|---|---|---|
| `Jwt:Key` | 256-bit JWT signing key (64 hex chars) | Random dev key |
| `Stripe:*` | Stripe keys (`sk_test_dummy*` = local simulation mode) | Simulation |
| `Email:*` | SMTP host/credentials | No-op when host empty |
| `Sms:*` | Twilio-style SMS | No-op when disabled |
| `Push:*` | FCM-style push | No-op when disabled |
| `Tracing:Enabled` | OpenTelemetry OTLP export | `false` in Test env |
| `ConnectionStrings:DefaultConnection` | SQL Server connection | localhost, Windows auth |

> **Important**: secrets are dev placeholders. Never ship real keys in committed config — use environment variables / secret management in production.

## Testing

```powershell
dotnet test
```

## Deployment

Docker and Kubernetes artifacts are in `deploy/`. See `docs/` for architecture details.