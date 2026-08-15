# Layer Dependency Verification

Rules to enforce:

- `Ecommerce.Domain` must not reference: ASP.NET Core, EF Core, Infrastructure projects, or any external SDKs.
- `Ecommerce.Application` must reference `Ecommerce.Domain` only, and must not reference Infrastructure implementations (only interfaces defined in Application are allowed).
- `Ecommerce.Infrastructure` may reference `Ecommerce.Application` and `Ecommerce.Domain` and external packages like `Microsoft.EntityFrameworkCore`.
- `Ecommerce.Api` may reference `Ecommerce.Application` and `Ecommerce.Domain` and should act as the composition root.

Automated checks (run after projects created):

- Ensure `Ecommerce.Domain` project file (`.csproj`) does not include package references to `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore.*`, or other infra packages.
- Ensure `Ecommerce.Application` does not reference `Ecommerce.Infrastructure` in project references.
- Use `dotnet list <project>.csproj reference` and `dotnet list <project>.csproj package` to validate references.

Example commands (to run later):

```powershell
dotnet list src/Ecommerce.Domain/Ecommerce.Domain.csproj package
dotnet list src/Ecommerce.Application/Ecommerce.Application.csproj reference
```

If accidental dependencies appear, remove them and move abstractions into `Ecommerce.Application`.

Verification status: not yet applicable — projects not created. Will run these checks as soon as projects are scaffolded.
