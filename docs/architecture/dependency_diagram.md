# Project Dependency Diagram

```mermaid
flowchart LR
  subgraph src
    Api[Ecommerce.Api]
    Application[Ecommerce.Application]
    Domain[Ecommerce.Domain]
    Infrastructure[Ecommerce.Infrastructure]
  end

  Api --> Application
  Application --> Domain
  Infrastructure --> Application
  Infrastructure --> Domain

  subgraph tests
    DomainTests[Ecommerce.Domain.Tests]
    ApplicationTests[Ecommerce.Application.Tests]
    IntegrationTests[Ecommerce.IntegrationTests]
  end

  DomainTests --> Domain
  ApplicationTests --> Application
  IntegrationTests --> Api
  IntegrationTests --> Infrastructure
```

Notes:
- Directional dependencies: Api -> Application -> Domain.
- Infrastructure implements interfaces defined in `Ecommerce.Application` and depends on both `Application` and `Domain`.
- Tests target the appropriate projects (unit tests for Domain/Application; integration tests for Api+Infrastructure).
