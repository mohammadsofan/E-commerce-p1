# Domain Rules, Application Use Cases, Infrastructure Services, External Dependencies, Security Notes

## Domain Rules (High-level)
- Aggregates own their invariants and enforce business rules within entities.
- Product aggregate ensures price integrity: `BasePrice >= 0`, `CostPrice >= 0`.
- ProductVariant must have unique `Sku` and non-negative `Price`.
- Inventory invariant: `QuantityOnHand >= 0`, `QuantityReserved >= 0`, `QuantityReserved <= QuantityOnHand`.
- Reservation invariant: reservations reduce available stock atomically using `RowVersion` optimistic concurrency; oversell must be prevented.
- Order aggregate invariants:
  - `TotalAmount == Subtotal - Discount + Shipping + Tax`
  - OrderItem snapshots capture product/variant name/sku/prices at placement time
  - Status transitions validated (e.g., cannot move from `Cancelled` to `Completed`)
- Coupon/Promotion rules validated in Application layer before applying.
- Pricing rules (discounts, promotions, taxes) computed server-side in Application; never trust client totals.
- Idempotency: checkout/payment endpoints must be idempotent using `IdempotencyKey` to avoid duplicate orders or payments.
- Soft deletes: querying hides `IsDeleted` entities via global filters; historical data retained.
- Sensitive operations (inventory adjustments, refunds, price changes) must be audited.

## Application Use Cases (by feature)
- Auth: Register, Login (JWT + Refresh), ForgotPassword, VerifyEmail, ManageProfile
- Products: Create/Update/Delete/ProductDetails/List/Search/Pagination
- Categories/Brands: CRUD, nesting, assignments
- Catalog: Add images/videos, manage attributes and variants
- Cart: Create cart, add/remove/update items, calculate totals
- Checkout: Validate cart, apply promotions, calculate tax/shipping, reserve stock, create order, process payment
- Orders: Get order, list orders (with filters/pagination), cancel, refund
- Payments: Authorize, Capture, Refund, Webhook handling, Idempotency
- Inventory: View stock per warehouse, reserve stock, release reservations, transfers
- Shipping: Create shipment, add tracking, partial shipments
- Coupons/Promotions: Validate and apply rules, usage tracking
- Reviews/Q&A: Create review (verified purchase check), moderate, vote
- Returns: Request return, approve/reject, process refund
- Notifications: Create/send notifications, read/mark-as-read
- Admin: Manage products, inventory, users, orders, coupons, promotions, audit logs

Each use case should be implemented as an Application feature (CQRS command/query) with validators and unit tests.

## Infrastructure Services and External Dependencies
- Data store: SQL Server (EF Core migrations live in Infrastructure)
- Identity: ASP.NET Core Identity (Identity tables in Infrastructure DbContext)
- Payments: Stripe, PayPal — implemented behind `IPaymentGateway` interface in Application
- Email: SMTP / SendGrid behind `IEmailService`
- File storage: AWS S3 / Azure Blob behind `IFileStorageService`
- Caching: Redis behind `ICacheService`
- Background jobs: Hangfire / Azure Functions / Worker Service for asynchronous tasks (webhooks, emails, long-running processes)
- Search: ElasticSearch / Azure Cognitive Search (optional) behind repository/service interfaces
- Monitoring & Logging: Application Insights / Seq / ELK stack
- Secrets: Azure Key Vault / AWS Secrets Manager / environment variables

## Security-sensitive Information
- Secrets and credentials: DB connection strings, API keys, payment provider secrets, SMTP credentials — store in secret manager (never in repo)
- PII: Emails, names, addresses — protect in transit (TLS) and at rest as required
- Payment data: Never store full PAN, CVV, or provider secrets. Use tokenization.
- Tokens: Do not log JWTs, refresh tokens, or payment tokens.
- Audit logs: Do not record secret values; mask or redact sensitive fields.

## Potential Circular Dependencies & Mitigations
- Avoid referencing Infrastructure from Application or Domain. Application defines interfaces; Infrastructure implements them and references Application and Domain.
- Use `Dependency Injection` and inversion of control to inject implementations at composition root (`Ecommerce.Api` or a separate composition project).
- Keep DTOs and Application interfaces defined in `Ecommerce.Application` to prevent API<>Domain coupling.

## Observability & Operational Notes
- Centralized exception middleware returning RFC 7807 `ProblemDetails`.
- Structured logging with correlation IDs per request.
- Health checks and readiness probes in `Ecommerce.Api`.
- Migrations run from Infrastructure project only; CI should apply migrations to test databases.

---

This document will be used to drive implementation tasks and tests. Next: verify layer dependency rules and then scaffold the solution and projects.