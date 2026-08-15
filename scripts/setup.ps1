Param(
    [string]$SolutionName = "Ecommerce",
    [string]$StartupProject = "src/Ecommerce.Api/Ecommerce.Api.csproj",
    [string]$InfrastructureProject = "src/Ecommerce.Infrastructure/Ecommerce.Infrastructure.csproj"
)

Write-Host "==> Creating solution and adding projects" -ForegroundColor Cyan
dotnet new sln -n $SolutionName
dotnet sln add src/Ecommerce.Domain/Ecommerce.Domain.csproj
dotnet sln add src/Ecommerce.Application/Ecommerce.Application.csproj
dotnet s l add src/Ecommerce.Infrastructure/Ecommerce.Infrastructure.csproj
dotnet sln add src/Ecommerce.Api/Ecommerce.Api.csproj
dotnet sln add tests/Ecommerce.Domain.Tests/Ecommerce.Domain.Tests.csproj
dotnet sln add tests/Ecommerce.Application.Tests/Ecommerce.Application.Tests.csproj
dotnet sln add tests/Ecommerce.IntegrationTests/Ecommerce.IntegrationTests.csproj

Write-Host "==> Adding EF Core packages to Infrastructure project" -ForegroundColor Cyan
dotnet add $InfrastructureProject package Microsoft.EntityFrameworkCore.SqlServer
dotnet add $InfrastructureProject package Microsoft.EntityFrameworkCore.Design

Write-Host "==> Restoring and building solution" -ForegroundColor Cyan
dotnet restore
dotnet build

Write-Host "==> Ensure dotnet-ef is available (global tool)" -ForegroundColor Yellow
if (-not (Get-Command dotnet-ef -ErrorAction SilentlyContinue)) {
    Write-Host "dotnet-ef not found. Installing globally..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
}

Write-Host "==> Creating initial migration (Infrastructure project)" -ForegroundColor Cyan
Push-Location src/Ecommerce.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ..\..\src\Ecommerce.Api\Ecommerce.Api.csproj
dotnet ef database update --startup-project ..\..\src\Ecommerce.Api\Ecommerce.Api.csproj
Pop-Location

Write-Host "==> Running tests" -ForegroundColor Cyan
dotnet test --no-build

Write-Host "Setup script finished." -ForegroundColor Green
