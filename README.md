# p1 — Clean Architecture ASP.NET API scaffold

This repository contains the folder scaffold for an ASP.NET Core Web API using Clean Architecture.

Folder layout (empty for now):

- src/
  - Api/           -> Web API project (presentation layer)
  - Application/   -> Use cases, DTOs, interfaces
  - Domain/        -> Entities, value objects, domain services
  - Infrastructure/-> DB, external services, EF Core, identity
  - SharedKernel/  -> Cross-cutting/shared domain primitives
- tests/           -> Unit and integration tests
- docs/            -> Architecture notes and diagrams

Next steps:

1. Run `dotnet new sln` and create projects inside `src/` with `dotnet new webapi`, `dotnet new classlib`, etc.
2. Add projects to the solution via `dotnet sln add`.
3. Create initial `csproj` files and minimal code when ready.

If you want, I can now create the solution and empty project files (`.csproj`) for each layer — tell me whether to proceed.# E-commerce-p1
