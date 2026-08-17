# syntax=docker/dockerfile:1

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore dependencies with layer caching
COPY Ecommerce.sln ./
COPY Directory.Build.props ./
COPY src/Ecommerce.Domain/Ecommerce.Domain.csproj src/Ecommerce.Domain/
COPY src/Ecommerce.Application/Ecommerce.Application.csproj src/Ecommerce.Application/
COPY src/Ecommerce.Infrastructure/Ecommerce.Infrastructure.csproj src/Ecommerce.Infrastructure/
COPY src/Ecommerce.Api/Ecommerce.Api.csproj src/Ecommerce.Api/
COPY tests/Ecommerce.Domain.Tests/Ecommerce.Domain.Tests.csproj tests/Ecommerce.Domain.Tests/
COPY tests/Ecommerce.Application.Tests/Ecommerce.Application.Tests.csproj tests/Ecommerce.Application.Tests/
COPY tests/Ecommerce.IntegrationTests/Ecommerce.IntegrationTests.csproj tests/Ecommerce.IntegrationTests/
RUN dotnet restore src/Ecommerce.Api/Ecommerce.Api.csproj

# Build & publish
COPY src/ src/
RUN dotnet publish src/Ecommerce.Api/Ecommerce.Api.csproj -c Release -o /app/publish --no-restore

# Run tests in a separate stage (optional; uncomment to run during build)
# FROM build AS test
# RUN dotnet test tests/Ecommerce.Domain.Tests/Ecommerce.Domain.Tests.csproj -c Release --no-restore \
#     && dotnet test tests/Ecommerce.Application.Tests/Ecommerce.Application.Tests.csproj -c Release --no-restore \
#     && dotnet test tests/Ecommerce.IntegrationTests/Ecommerce.IntegrationTests.csproj -c Release --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Run as the built-in non-root user provided by the aspnet runtime image
USER appuser

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
EXPOSE 9090

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Ecommerce.Api.dll"]