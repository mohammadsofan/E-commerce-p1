#!/usr/bin/env bash
set -euo pipefail

SOLUTION_NAME="Ecommerce"
STARTUP_PROJECT="src/Ecommerce.Api/Ecommerce.Api.csproj"
INFRA_PROJECT="src/Ecommerce.Infrastructure/Ecommerce.Infrastructure.csproj"

echo "==> Creating solution and adding projects"
dotnet new sln -n "$SOLUTION_NAME"
dotnet sln add src/Ecommerce.Domain/Ecommerce.Domain.csproj
dotnet sln add src/Ecommerce.Application/Ecommerce.Application.csproj
dotnet s l add src/Ecommerce.Infrastructure/Ecommerce.Infrastructure.csproj
dotnet sln add src/Ecommerce.Api/Ecommerce.Api.csproj
dotnet sln add tests/Ecommerce.Domain.Tests/Ecommerce.Domain.Tests.csproj
dotnet sln add tests/Ecommerce.Application.Tests/Ecommerce.Application.Tests.csproj
dotnet sln add tests/Ecommerce.IntegrationTests/Ecommerce.IntegrationTests.csproj

echo "==> Adding EF Core packages to Infrastructure project"
dotnet add "$INFRA_PROJECT" package Microsoft.EntityFrameworkCore.SqlServer
dotnet add "$INFRA_PROJECT" package Microsoft.EntityFrameworkCore.Design

echo "==> Adding AutoMapper and FluentValidation packages"
dotnet add src/Ecommerce.Application/Ecommerce.Application.csproj package AutoMapper
dotnet add src/Ecommerce.Application/Ecommerce.Application.csproj package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add src/Ecommerce.Api/Ecommerce.Api.csproj package FluentValidation.AspNetCore
dotnet add src/Ecommerce.Application/Ecommerce.Application.csproj package FluentValidation

echo "==> Restoring and building"
dotnet restore
dotnet build

if ! command -v dotnet-ef >/dev/null 2>&1; then
  echo "dotnet-ef not found. Installing..."
  dotnet tool install --global dotnet-ef
fi

echo "==> Creating initial migration"
pushd src/Ecommerce.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../../src/Ecommerce.Api/Ecommerce.Api.csproj
dotnet ef database update --startup-project ../../src/Ecommerce.Api/Ecommerce.Api.csproj
popd

echo "==> Running tests"
dotnet test --no-build

echo "Setup complete." 
