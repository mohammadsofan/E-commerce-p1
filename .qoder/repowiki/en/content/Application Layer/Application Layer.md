# Application Layer

<cite>
**Referenced Files in This Document**
- [CheckoutCommand.cs](file://src/Ecommerce.Application/Commands/Checkout/CheckoutCommand.cs)
- [CheckoutCommandHandler.cs](file://src/Ecommerce.Application/Commands/Checkout/CheckoutCommandHandler.cs)
- [ReserveInventoryCommand.cs](file://src/Ecommerce.Application/Commands/ReserveInventory/ReserveInventoryCommand.cs)
- [ReserveInventoryCommandHandler.cs](file://src/Ecommerce.Application/Commands/ReserveInventory/ReserveInventoryCommandHandler.cs)
- [CommandDispatcher.cs](file://src/Ecommerce.Application/Common/Commands/CommandDispatcher.cs)
- [ICommand.cs](file://src/Ecommerce.Application/Common/Commands/ICommand.cs)
- [ICommandHandler.cs](file://src/Ecommerce.Application/Common/Commands/ICommandHandler.cs)
- [LoggingBehavior.cs](file://src/Ecommerce.Application/Common/Commands/LoggingBehavior.cs)
- [ValidationBehavior.cs](file://src/Ecommerce.Application/Common/Commands/ValidationBehavior.cs)
- [FluentValidationAdapter.cs](file://src/Ecommerce.Application/Common\Validation/FluentValidationAdapter.cs)
- [CheckoutCommandFluentValidator.cs](file://src/Ecommerce.Application/Validators/CheckoutCommandFluentValidator.cs)
- [ReserveInventoryFluentValidator.cs](file://src/Ecommerce.Application/Validators/ReserveInventoryFluentValidator.cs)
- [OrderDto.cs](file://src/Ecommerce.Application/DTOs/OrderDto.cs)
- [ProductDto.cs](file://src/Ecommerce.Application/DTOs/ProductDto.cs)
- [MappingProfile.cs](file://src/Ecommerce.Application/Mappings/MappingProfile.cs)
</cite>

## Table of Contents
1. Introduction
2. Project Structure
3. Core Components
4. Architecture Overview
5. Detailed Component Analysis
6. Dependency Analysis
7. Performance Considerations
8. Troubleshooting Guide
9. Conclusion

## Introduction
This document explains the Application Layer that implements a Command Query Responsibility Segregation (CQRS) pattern for an e-commerce system. It focuses on command handlers such as CheckoutCommandHandler and ReserveInventoryCommandHandler, the command dispatcher, validation behaviors, logging mechanisms, DTOs, AutoMapper configuration, FluentValidation usage, transaction management, error handling, and cross-cutting concerns. The goal is to show how the application layer coordinates domain operations while maintaining clear separation of concerns.

## Project Structure
The Application Layer is organized around commands and their handlers, common abstractions for dispatching and behaviors, validators, DTOs, and mapping profiles. Commands represent write operations; handlers orchestrate workflows by interacting with domain entities and persistence through interfaces. Behaviors wrap handler execution to provide cross-cutting functionality like validation and logging.

```mermaid
graph TB
subgraph "Application Layer"
CD["CommandDispatcher"]
CBV["ValidationBehavior<T,R>"]
CLB["LoggingBehavior<T,R>"]
CH1["CheckoutCommandHandler"]
CH2["ReserveInventoryCommandHandler"]
V1["CheckoutCommandFluentValidator"]
V2["ReserveInventoryFluentValidator"]
FA["FluentValidationAdapter<T>"]
D1["OrderDto"]
D2["ProductDto"]
MP["MappingProfile"]
end
CD --> CBV
CD --> CLB
CD --> CH1
CD --> CH2
CBV --> FA
FA --> V1
FA --> V2
CH1 --> D1
CH2 --> D1
MP --> D1
MP --> D2
```

**Diagram sources**
- [CommandDispatcher.cs:1-47](file://src/Ecommerce.Application/Common/Commands/CommandDispatcher.cs#L1-L47)
- [ValidationBehavior.cs:1-41](file://src/Ecommerce.Application/Common/Commands/ValidationBehavior.cs#L1-L41)
- [LoggingBehavior.cs:1-34](file://src/Ecommerce.Application/Common/Commands/LoggingBehavior.cs#L1-L34)
- [CheckoutCommandHandler.cs:1-94](file://src/Ecommerce.Application/Commands/Checkout/CheckoutCommandHandler.cs#L1-L94)
- [ReserveInventoryCommandHandler.cs:1-31](file://src/Ecommerce.Application/Commands/ReserveInventory/ReserveInventoryCommandHandler.cs#L1-L31)
- [CheckoutCommandFluentValidator.cs:1-19](file://src/Ecommerce.Application/Validators/CheckoutCommandFluentValidator.cs#L1-L19)
- [ReserveInventoryFluentValidator.cs:1-14](file://src/Ecommerce.Application/Validators/ReserveInventoryFluentValidator.cs#L1-L14)
- [FluentValidationAdapter.cs:1-28](file://src/Ecommerce.Application/Common\Validation/FluentValidationAdapter.cs#L1-L28)
- [OrderDto.cs:1-22](file://src/Ecommerce.Application/DTOs/OrderDto.cs#L1-L22)
- [ProductDto.cs:1-13](file://src/Ecommerce.Application/DTOs/ProductDto.cs#L1-L13)
- [MappingProfile.cs:1-30](file://src/Ecommerce.Application/Mappings/MappingProfile.cs#L1-L30)

**Section sources**
- [CommandDispatcher.cs:1-47](file://src/Ecommerce.Application/Common/Commands/CommandDispatcher.cs#L1-L47)
- [CheckoutCommandHandler.cs:1-94](file://src/Ecommerce.Application/Commands/Checkout/CheckoutCommandHandler.cs#L1-L94)
- [ReserveInventoryCommandHandler.cs:1-31](file://src/Ecommerce.Application/Commands/ReserveInventory/ReserveInventoryCommandHandler.cs#L1-L31)

## Core Components
- Command Dispatcher: Resolves handlers and executes them within a pipeline of behaviors for validation and logging.
- Handlers: Implement business workflows for specific commands.
- Validation Pipeline: Uses FluentValidation via an adapter and a behavior to validate commands before handler execution.
- Logging Pipeline: Logs entry and exit of command handling and captures exceptions.
- DTOs and Mapping: Define data transfer structures and AutoMapper mappings between domain entities and DTOs.

Key responsibilities:
- Keep handlers focused on orchestrating domain operations without leaking infrastructure details.
- Centralize cross-cutting concerns in behaviors.
- Provide consistent validation and logging across all commands.

**Section sources**
- [CommandDispatcher.cs:1-47](file://src/Ecommerce.Application/Common/Commands/CommandDispatcher.cs#L1-L47)
- [ValidationBehavior.cs:1-41](file://src/Ecommerce.Application/Common/Commands/ValidationBehavior.cs#L1-L41)
- [LoggingBehavior.cs:1-34](file://src/Ecommerce.Application/Common/Commands/LoggingBehavior.cs#L1-L34)
- [FluentValidationAdapter.cs:1-28](file://src/Ecommerce.Application/Common\Validation/FluentValidationAdapter.cs#L1-L28)
- [OrderDto.cs:1-22](file://src/Ecommerce.Application/DTOs/OrderDto.cs#L1-L22)
- [ProductDto.cs:1-13](file://src/Ecommerce.Application/DTOs/ProductDto.cs#L1-L13)
- [MappingProfile.cs:1-30](file://src/Ecommerce.Application/Mappings/MappingProfile.cs#L1-L30)

## Architecture Overview
The command flow uses a dispatcher to locate the appropriate handler and execute it through a pipeline of behaviors. Validation occurs first, then logging wraps the entire operation. Handlers interact with domain entities and persistence via interfaces, ensuring separation from infrastructure.

```mermaid
sequenceDiagram
participant Client as "Caller"
participant Dispatcher as "CommandDispatcher"
participant VBeh as "ValidationBehavior"
participant LBeh as "LoggingBehavior"
participant Handler as "CheckoutCommandHandler"
participant DB as "IApplicationDbContext"
participant Idem as "IIdempotencyService"
Client->>Dispatcher : Send(CheckoutCommand)
Dispatcher->>VBeh : Handle(command, next)
VBeh->>VBeh : Resolve validators and validate
VBeh-->>Dispatcher : Continue if valid
Dispatcher->>LBeh : Handle(command, next)
LBeh->>Handler : Handle(command)
Handler->>Idem : Check/Register idempotency key
Handler->>DB : Load inventory items
Handler->>Handler : Build Order and reserve inventory
Handler->>DB : Persist order
Handler-->>LBeh : Return result
LBeh-->>Dispatcher : Return result
Dispatcher-->>Client : Result
```

**Diagram sources**
- [CommandDispatcher.cs:1-47](file://src/Ecommerce.Application/Common/Commands/CommandDispatcher.cs#L1-L47)
- [ValidationBehavior.cs:1-41](file://src/Ecommerce.Application/Common/Commands/ValidationBehavior.cs#L1-L41)
- [LoggingBehavior.cs:1-34](file://src/Ecommerce.Application/Common/Commands/LoggingBehavior.cs#L1-L34)
- [CheckoutCommandHandler.cs:1-94](file://src/Ecommerce.Application/Commands/Checkout/CheckoutCommandHandler.cs#L1-L94)

## Detailed Component Analysis

### Command Dispatcher
- Purpose: Locates the correct ICommandHandler for a given command and executes it through a pipeline of behaviors.
- Behavior resolution: Retrieves registered ICommandBehavior implementations and composes them around the handler call.
- Logging: Logs command start and completion at the dispatcher level.

```mermaid
flowchart TD
Start(["Send(command)"]) --> Resolve["Resolve ICommandHandler<TCommand,TResult>"]
Resolve --> HasHandler{"Handler found?"}
HasHandler -- No --> ThrowErr["Throw InvalidOperationException"]
HasHandler -- Yes --> GetBehaviors["Resolve IEnumerable<ICommandBehavior<TCommand,TResult>>"]
GetBehaviors --> Compose["Compose pipeline: behaviors around handler delegate"]
Compose --> Execute["Execute pipeline"]
Execute --> LogEnd["Log completion"]
LogEnd --> End(["Return result"])
```

**Diagram sources**
- [CommandDispatcher.cs:1-47](file://src/Ecommerce.Application/Common/Commands/CommandDispatcher.cs#L1-L47)

**Section sources**
- [CommandDispatcher.cs:1-47](file://src/Ecommerce.Application/Common/Commands/CommandDispatcher.cs#L1-L47)

### Validation Pipeline
- Strategy: A behavior resolves all IValidator<TCommand> instances and validates the command before invoking the handler.
- Adapter: FluentValidation validators are wrapped by FluentValidationAdapter to conform to the project’s IValidator abstraction.
- Error propagation: Validation failures are aggregated and thrown as a domain exception, stopping handler execution.

```mermaid
flowchart TD
Enter(["ValidationBehavior.Handle"]) --> ResolveVals["Resolve validators for TCommand"]
ResolveVals --> ValidateAll["Validate command with each validator"]
ValidateAll --> AnyErrors{"Any errors?"}
AnyErrors -- Yes --> ThrowDomain["Aggregate errors and throw DomainException"]
AnyErrors -- No --> Next["Invoke next in pipeline"]
Next --> Exit(["Return to caller"])
```

**Diagram sources**
- [ValidationBehavior.cs:1-41](file://src/Ecommerce.Application/Common/Commands/ValidationBehavior.cs#L1-L41)
- [FluentValidationAdapter.cs:1-28](file://src/Ecommerce.Application/Common\Validation/FluentValidationAdapter.cs#L1-L28)

**Section sources**
- [ValidationBehavior.cs:1-41](file://src/Ecommerce.Application/Common/Commands/ValidationBehavior.cs#L1-L41)
- [FluentValidationAdapter.cs:1-28](file://src/Ecommerce.Application/Common\Validation/FluentValidationAdapter.cs#L1-L28)
- [CheckoutCommandFluentValidator.cs:1-19](file://src/Ecommerce.Application/Validators/CheckoutCommandFluentValidator.cs#L1-L19)
- [ReserveInventoryFluentValidator.cs:1-14](file://src/Ecommerce.Application/Validators/ReserveInventoryFluentValidator.cs#L1-L14)

### Logging Pipeline
- Purpose: Wraps handler execution to log entry, successful completion, and any exceptions.
- Exception handling: Re-throws exceptions after logging to preserve failure semantics.

```mermaid
flowchart TD
Start(["LoggingBehavior.Handle"]) --> LogStart["Log command start"]
LogStart --> TryNext["Invoke next()"]
TryNext --> Success{"Success?"}
Success -- Yes --> LogDone["Log command done"]
Success -- No --> LogError["Log exception"]
LogError --> Rethrow["Rethrow exception"]
LogDone --> Exit(["Return result"])
```

**Diagram sources**
- [LoggingBehavior.cs:1-34](file://src/Ecommerce.Application/Common/Commands/LoggingBehavior.cs#L1-L34)

**Section sources**
- [LoggingBehavior.cs:1-34](file://src/Ecommerce.Application/Common/Commands/LoggingBehavior.cs#L1-L34)

### CheckoutCommandHandler
- Responsibilities:
  - Enforce idempotency using IIdempotencyService when an idempotency key is provided.
  - Validate input and build an Order entity.
  - Reserve inventory for each item by loading InventoryItem and calling its Reserve method.
  - Persist the order via IApplicationDbContext.
  - Save the idempotency response upon success.
- Error handling: Throws domain-specific exceptions for invalid inputs or missing inventory.

```mermaid
sequenceDiagram
participant H as "CheckoutCommandHandler"
participant Idem as "IIdempotencyService"
participant DB as "IApplicationDbContext"
participant O as "Order"
participant Inv as "InventoryItem"
H->>Idem : TryGetResponseAsync(idempotencyKey)
alt Key exists and has response
Idem-->>H : Found with response
H-->>H : Return previous order id
else No prior response
H->>Idem : TryRegisterAsync(key, hash, userId)
alt Registration fails
H-->>H : Throw DomainException
end
end
H->>H : Validate items not empty
loop For each item
H->>DB : Find InventoryItem by variant/product id
DB-->>H : InventoryItem
H->>Inv : Reserve(quantity)
end
H->>O : Add items and PlaceOrder
H->>DB : Add and SaveChanges
H->>Idem : SaveResponseAsync(key, orderId)
H-->>H : Return orderId
```

**Diagram sources**
- [CheckoutCommandHandler.cs:1-94](file://src/Ecommerce.Application/Commands/Checkout/CheckoutCommandHandler.cs#L1-L94)

**Section sources**
- [CheckoutCommandHandler.cs:1-94](file://src/Ecommerce.Application/Commands/Checkout/CheckoutCommandHandler.cs#L1-L94)
- [CheckoutCommand.cs:1-22](file://src/Ecommerce.Application/Commands/Checkout/CheckoutCommand.cs#L1-L22)

### ReserveInventoryCommandHandler
- Responsibilities:
  - Validate quantity and existence of the inventory item.
  - Reserve the requested quantity on the domain entity.
  - Persist changes via IApplicationDbContext.
- Error handling: Throws domain-specific exceptions for invalid quantities or missing items.

```mermaid
sequenceDiagram
participant H as "ReserveInventoryCommandHandler"
participant DB as "IApplicationDbContext"
participant Inv as "InventoryItem"
H->>H : Validate Quantity > 0
H->>DB : Find InventoryItem
DB-->>H : InventoryItem or null
alt Not found
H-->>H : Throw InventoryException
end
H->>Inv : Reserve(Quantity)
H->>DB : SaveChanges
H-->>H : Return Unit
```

**Diagram sources**
- [ReserveInventoryCommandHandler.cs:1-31](file://src/Ecommerce.Application/Commands/ReserveInventory/ReserveInventoryCommandHandler.cs#L1-L31)

**Section sources**
- [ReserveInventoryCommandHandler.cs:1-31](file://src/Ecommerce.Application/Commands/ReserveInventory/ReserveInventoryCommandHandler.cs#L1-L31)

### DTOs and AutoMapper Configuration
- DTOs:
  - OrderDto and OrderItemDto represent read models for orders and their items.
  - ProductDto represents product information for queries.
- AutoMapper:
  - MappingProfile defines mappings from domain entities (Order, OrderItem, Product) to DTOs.
  - Mappings ensure consistent projection of domain state into API responses.

```mermaid
classDiagram
class Order {
+Guid Id
+string OrderNumber
+decimal TotalAmount
+List<OrderItem> Items
}
class OrderItem {
+Guid ProductId
+Guid ProductVariantId
+int Quantity
+decimal UnitPrice
}
class Product {
+Guid Id
+string Name
+string Slug
+decimal BasePrice
}
class OrderDto {
+Guid Id
+string OrderNumber
+decimal TotalAmount
+List<OrderItemDto> Items
}
class OrderItemDto {
+Guid ProductId
+Guid ProductVariantId
+int Quantity
+decimal UnitPrice
}
class ProductDto {
+Guid Id
+string Name
+string Slug
+decimal BasePrice
}
Order --> OrderItem : "has many"
Order <.. OrderDto : "maps to"
OrderItem <.. OrderItemDto : "maps to"
Product <.. ProductDto : "maps to"
```

**Diagram sources**
- [OrderDto.cs:1-22](file://src/Ecommerce.Application/DTOs/OrderDto.cs#L1-L22)
- [ProductDto.cs:1-13](file://src/Ecommerce.Application/DTOs/ProductDto.cs#L1-L13)
- [MappingProfile.cs:1-30](file://src/Ecommerce.Application/Mappings/MappingProfile.cs#L1-L30)

**Section sources**
- [OrderDto.cs:1-22](file://src/Ecommerce.Application/DTOs/OrderDto.cs#L1-L22)
- [ProductDto.cs:1-13](file://src/Ecommerce.Application/DTOs/ProductDto.cs#L1-L13)
- [MappingProfile.cs:1-30](file://src/Ecommerce.Application/Mappings/MappingProfile.cs#L1-L30)

### Creating New Commands and Handlers
Follow these patterns to add new commands:
- Define a command type in a feature folder under Commands.
- Create a handler implementing ICommandHandler<TCommand, TResult>.
- Add a FluentValidator for the command and register it so the ValidationBehavior can resolve it.
- Use the CommandDispatcher.Send<TCommand, TResult> to invoke the command from higher layers.
- Ensure handlers only coordinate domain logic and persist via IApplicationDbContext.

Example steps:
- Create MyFeatureCommand and MyFeatureCommandHandler.
- Add MyFeatureCommandFluentValidator and wire it up via dependency injection.
- In your API controller or service, call CommandDispatcher.Send(new MyFeatureCommand(), cancellationToken).

**Section sources**
- [ICommandHandler.cs:1-11](file://src/Ecommerce.Application/Common/Commands/ICommandHandler.cs#L1-L11)
- [CommandDispatcher.cs:1-47](file://src/Ecommerce.Application/Common/Commands/CommandDispatcher.cs#L1-L47)
- [ValidationBehavior.cs:1-41](file://src/Ecommerce.Application/Common/Commands/ValidationBehavior.cs#L1-L41)
- [FluentValidationAdapter.cs:1-28](file://src/Ecommerce.Application/Common\Validation/FluentValidationAdapter.cs#L1-L28)

## Dependency Analysis
- CommandDispatcher depends on:
  - Service provider to resolve handlers and behaviors.
  - ILogger for logging.
- Handlers depend on:
  - IApplicationDbContext for persistence.
  - IIdempotencyService for idempotency support (in Checkout).
- ValidationBehavior depends on:
  - IServiceProvider to resolve validators.
  - IValidator<TCommand> implementations (via FluentValidationAdapter).
- LoggingBehavior depends on:
  - ILogger for structured logs.

```mermaid
graph LR
CD["CommandDispatcher"] --> IH["ICommandHandler<T,R>"]
CD --> IB["ICommandBehavior<T,R>"]
IB --> VAL["IValidator<T>"]
VAL --> FVA["FluentValidationAdapter<T>"]
FVA --> FLV["FluentValidation.IValidator<T>"]
CH["Handlers"] --> DB["IApplicationDbContext"]
CH --> IDEM["IIdempotencyService"]
LB["LoggingBehavior<T,R>"] --> LOG["ILogger"]
```

**Diagram sources**
- [CommandDispatcher.cs:1-47](file://src/Ecommerce.Application/Common/Commands/CommandDispatcher.cs#L1-L47)
- [ValidationBehavior.cs:1-41](file://src/Ecommerce.Application/Common/Commands/ValidationBehavior.cs#L1-L41)
- [FluentValidationAdapter.cs:1-28](file://src/Ecommerce.Application/Common\Validation/FluentValidationAdapter.cs#L1-L28)
- [LoggingBehavior.cs:1-34](file://src/Ecommerce.Application/Common/Commands/LoggingBehavior.cs#L1-L34)
- [CheckoutCommandHandler.cs:1-94](file://src/Ecommerce.Application/Commands/Checkout/CheckoutCommandHandler.cs#L1-L94)
- [ReserveInventoryCommandHandler.cs:1-31](file://src/Ecommerce.Application/Commands/ReserveInventory/ReserveInventoryCommandHandler.cs#L1-L31)

**Section sources**
- [CommandDispatcher.cs:1-47](file://src/Ecommerce.Application/Common/Commands/CommandDispatcher.cs#L1-L47)
- [ValidationBehavior.cs:1-41](file://src/Ecommerce.Application/Common/Commands/ValidationBehavior.cs#L1-L41)
- [LoggingBehavior.cs:1-34](file://src/Ecommerce.Application/Common/Commands/LoggingBehavior.cs#L1-L34)
- [CheckoutCommandHandler.cs:1-94](file://src/Ecommerce.Application/Commands/Checkout/CheckoutCommandHandler.cs#L1-L94)
- [ReserveInventoryCommandHandler.cs:1-31](file://src/Ecommerce.Application/Commands/ReserveInventory/ReserveInventoryCommandHandler.cs#L1-L31)

## Performance Considerations
- Idempotency: Prevents duplicate processing of checkout requests by caching request hashes and results.
- Validation early exit: ValidationBehavior stops further processing when errors are found, reducing unnecessary work.
- Logging overhead: LoggingBehavior adds minimal overhead but provides valuable diagnostics; consider log levels in production.
- Database access: Handlers perform targeted lookups and updates; ensure indexes exist for frequently queried keys (e.g., product variant IDs).

[No sources needed since this section provides general guidance]

## Troubleshooting Guide
Common issues and resolutions:
- No handler registered: Occurs when a command lacks a corresponding handler. Ensure the handler is implemented and registered in DI.
- Validation failures: Aggregated errors are thrown as a domain exception. Check FluentValidation rules and messages.
- Missing inventory: Handlers throw domain exceptions when inventory cannot be found or reserved. Verify inventory records exist and have sufficient stock.
- Idempotency conflicts: If registration fails, the handler throws a domain exception indicating the request is already in flight. Retry with backoff or check existing response.
- Persistence errors: SaveChanges may fail due to constraints or concurrency. Wrap calls in transactions and handle exceptions appropriately.

**Section sources**
- [CommandDispatcher.cs:1-47](file://src/Ecommerce.Application/Common/Commands/CommandDispatcher.cs#L1-L47)
- [ValidationBehavior.cs:1-41](file://src/Ecommerce.Application/Common/Commands/ValidationBehavior.cs#L1-L41)
- [CheckoutCommandHandler.cs:1-94](file://src/Ecommerce.Application/Commands/Checkout/CheckoutCommandHandler.cs#L1-L94)
- [ReserveInventoryCommandHandler.cs:1-31](file://src/Ecommerce.Application/Commands/ReserveInventory/ReserveInventoryCommandHandler.cs#L1-L31)

## Conclusion
The Application Layer implements CQRS with a clean separation between commands and handlers, robust validation and logging via behaviors, and clear DTO/mapping strategies. Handlers coordinate domain operations while remaining decoupled from infrastructure through interfaces. Idempotency and explicit error handling improve reliability. Following the established patterns ensures consistency and maintainability as new features are added.

[No sources needed since this section summarizes without analyzing specific files]