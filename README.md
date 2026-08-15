 # E-Commerce Backend — Clean Architecture

 This repository scaffolds a production-ready e-commerce backend using Clean Architecture.

 Implementation Plan (phases):

 1. Architecture (done)
 2. Domain: entities, value objects, domain events, exceptions (in progress)
 3. Application: interfaces, DTOs, CQRS features, validators
 4. Infrastructure: EF Core, Identity, repositories, services, migrations
 5. API: controllers, auth, middleware, swagger
 6. Catalog, Customer, Inventory, Checkout, Payments, Post-order, Admin

 Current status:
 - Architecture docs added in `docs/architecture/`.
 - Solution and project csproj scaffolding added in `src/` and `tests/` (manual csproj files; run `dotnet new sln` locally if needed).
 - CI workflow added: `.github/workflows/ci.yml`.
 - Domain entities skeletons added (Phase 2 started).

 Next steps:
 - Continue implementing Domain entities and value objects.
 - Implement Application interfaces and DTOs.
 - Implement Infrastructure DbContext and EF configurations.

 To run locally (once .NET SDK is installed):

 ```powershell
 dotnet new sln -n Ecommerce
 dotnet sln add src/Ecommerce.Api/Ecommerce.Api.csproj src/Ecommerce.Application/Ecommerce.Application.csproj src/Ecommerce.Domain/Ecommerce.Domain.csproj src/Ecommerce.Infrastructure/Ecommerce.Infrastructure.csproj tests/Ecommerce.Domain.Tests/Ecommerce.Domain.Tests.csproj tests/Ecommerce.Application.Tests/Ecommerce.Application.Tests.csproj tests/Ecommerce.IntegrationTests/Ecommerce.IntegrationTests.csproj

 dotnet restore
 ```

