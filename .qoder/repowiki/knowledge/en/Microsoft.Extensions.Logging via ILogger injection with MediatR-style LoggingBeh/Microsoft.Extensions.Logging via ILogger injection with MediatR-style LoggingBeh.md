---
kind: logging_system
name: Microsoft.Extensions.Logging via ILogger injection with MediatR-style LoggingBehavior
category: logging_system
scope:
    - '**'
source_files:
    - src/Ecommerce.Api/Program.cs
    - src/Ecommerce.Application/Common/Commands/LoggingBehavior.cs
    - src/Ecommerce.Application/Common/Commands/CommandDispatcher.cs
    - src/Ecommerce.Infrastructure/Services/RefreshTokenCleanupService.cs
    - tests/Ecommerce.Application.Tests/DispatcherReserveInventoryTests.cs
---

## What system/approach is used

The application uses **Microsoft.Extensions.Logging** (`ILogger<T>`) as its logging abstraction. There is no custom logger framework, Serilog, NLog, or third-party sink configured in the API project's `Program.cs`. The ASP.NET Core host supplies a default console (and development) logger pipeline through `WebApplication.CreateBuilder`, and consumers obtain loggers via constructor injection of `ILogger<T>`.

## Key files and packages

- `src/Ecommerce.Api/Program.cs` — Application entry point; builds the web host and registers services but does **not** call `AddLogging` explicitly (relying on ASP.NET Core defaults).
- `src/Ecommerce.Application/Common/Commands/LoggingBehavior.cs` — A cross-cutting behavior that wraps every command handler execution, emitting structured logs for start, completion, and errors.
- `src/Ecommerce.Application/Common/Commands/CommandDispatcher.cs` — Central dispatch point for commands; also emits `Information` logs around dispatch and completion.
- `src/Ecommerce.Infrastructure/Services/RefreshTokenCleanupService.cs` — Background service using `ILogger<RefreshTokenCleanupService>` to log periodic cleanup results and errors.
- `tests/Ecommerce.Application.Tests/DispatcherReserveInventoryTests.cs` — Test setup wires an in-memory `ILoggerFactory` / `Logger<>` so tests can capture emitted logs.

## Architecture and conventions

1. **Injection-based logging**: Every component that needs to log declares an `ILogger<T>` dependency in its constructor. No static logger instances are used anywhere in the codebase.
2. **Structured fields via named parameters**: All log calls use message templates with named placeholders (e.g. `"Handling {Command}"`, `"Dispatching command {CommandType}"`, `"Removed {Count} expired refresh tokens"`). This produces key-value structured fields rather than interpolated strings, enabling queryable logs.
3. **Cross-cutting command logging via a behavior**: The `LoggingBehavior<TCommand, TResult>` implements `ICommandBehavior<TCommand, TResult>` and is composed into the command pipeline built by `CommandDispatcher`. It logs:
   - `Information` before invoking the next delegate (command start)
   - `Information` after successful completion
   - `Error` with the exception when a handler throws
4. **Central dispatcher logging**: `CommandDispatcher.Send` additionally logs at dispatch time and after the full pipeline completes, providing an outer boundary trace around all command executions.
5. **Background service logging**: Long-running services like `RefreshTokenCleanupService` log routine outcomes and wrap their work in try/catch blocks that log exceptions at `Error` level.
6. **No explicit sink configuration**: The API project does not configure additional sinks, file output, correlation IDs, or log levels beyond what ASP.NET Core provides out of the box. Configuration would be expected from `appsettings.json` / environment variables if extended later.
7. **Test harness for logs**: Tests register `ILoggerFactory` and `ILogger<>` implementations so they can assert logged output during unit/integration scenarios.

## Conventions and constraints

- **Use `ILogger<T>` injected via constructor** — observed in `CommandDispatcher`, `LoggingBehavior<TCommand,TResult>`, and `RefreshTokenCleanupService`; no other logging mechanism is present.
- **Use message-template placeholders** (`{Name}`) for structured fields — all log statements follow this pattern consistently.
- **Wrap handler execution in try/catch and log at `Error` level** — both the `LoggingBehavior` and `RefreshTokenCleanupService` catch exceptions and emit `LogError(ex, ...)`.
- **Log command identity via `typeof(TCommand).FullName`** — the command type name is used as a contextual field across the dispatcher and behavior.
- **No global log-level or sink policy is enforced in code** — the only place where logging infrastructure could be customized is `Program.cs`, which currently leaves it to ASP.NET Core defaults.