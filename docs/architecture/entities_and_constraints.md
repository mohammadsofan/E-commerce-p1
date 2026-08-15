# Entities, Properties, Keys, Indexes, Delete Behavior, Concurrency

This document lists major domain entities, their properties, primary keys (PK), foreign keys (FK), unique constraints, suggested indexes, delete behaviors, and concurrency requirements. Use this as the source-of-truth for Domain model design and EF Core configurations (in Infrastructure). The Domain project contains only entity classes and value objects (no EF Core references).

Guidelines:
- All PKs use `Guid` unless otherwise noted.
- Monetary values use `decimal` with precision (18,2).
- IDs are GUIDs represented as `Guid`.
- RowVersion (byte[]) used for optimistic concurrency where noted.

-----

## Identity / Users

### ApplicationUser
- PK: `Id (Guid)`
- Properties:
  - `UserName` (string)
  - `NormalizedUserName` (string)
  - `Email` (string)
  - `NormalizedEmail` (string)
  - `EmailConfirmed` (bool)
  - `PhoneNumber` (string)
  - `PhoneNumberConfirmed` (bool)
  - `FirstName` (string)
  - `LastName` (string)
  - `DisplayName` (string)
  - `ProfileImageUrl` (string)
  - `IsActive` (bool)
  - `CreatedAt` (DateTimeOffset)
  - `LastLoginAt` (DateTimeOffset?)
  - `IsEmailVerified` (bool)
  - `IsPhoneVerified` (bool)
- FKs: none (Identity tables managed by Identity schema)
- Uniques: `NormalizedUserName` (Identity), `NormalizedEmail` (Identity)
- Indexes: Identity defaults (NormalizedUserName, NormalizedEmail)
- DeleteBehavior: `Restrict` for related audit/order records (do not cascade)
- Concurrency: handled by Identity; include `RowVersion` if desired in custom user table (optional)

### ApplicationRole
- PK: `Id (Guid)`
- Properties: `Name`, `NormalizedName`, `Description`, `CreatedAt`
- Uniques: `NormalizedName`
- DeleteBehavior: Restrict for user-role links

### UserProfile
- PK: `Id (Guid)`
- FK: `UserId (Guid)` -> `ApplicationUser.Id`
- Properties: `FirstName`, `LastName`, `DisplayName`, `Gender`, `DateOfBirth`, `ProfileImageUrl`, `CreatedAt`, `UpdatedAt`
- Unique: `UserId` (one-to-one)
- Indexes: `UserId`
- DeleteBehavior: `Cascade` when user deleted? Prefer `Restrict` and rely on admin to remove profile explicitly.

### Address
- PK: `Id (Guid)`
- FK: `UserId (Guid)` -> `ApplicationUser.Id`
- Properties: `Type` (enum Billing/Shipping), `FirstName`, `LastName`, `CompanyName`, `AddressLine1`, `AddressLine2`, `City`, `State`, `PostalCode`, `CountryCode`, `PhoneNumber`, `IsDefaultShipping` (bool), `IsDefaultBilling` (bool), `CreatedAt`, `UpdatedAt`, `IsDeleted` (soft delete)
- Indexes: `UserId`, `IsDefaultShipping`, `IsDefaultBilling`
- DeleteBehavior: `Restrict` (do not cascade into orders) — orders store snapshots
- Concurrency: no

---

## Catalog

### Brand
- PK: `Id (Guid)`
- Properties: `Name`, `Slug`, `Description`, `ImageUrl`, `IsActive`, `CreatedAt`, `UpdatedAt`, `IsDeleted`
- Uniques: `Slug`
- Indexes: `Slug`, `IsActive`
- DeleteBehavior: `Restrict` (products reference brand)
- Concurrency: optional `RowVersion`

### Category
- PK: `Id (Guid)`
- Properties: `ParentCategoryId (Guid?)`, `Name`, `Slug`, `Description`, `ImageUrl`, `DisplayOrder` (int), `IsActive`, `IsFeatured`, `MetaTitle`, `MetaDescription`, `CreatedAt`, `UpdatedAt`, `IsDeleted`
- FKs: `ParentCategoryId` -> `Category.Id` (self-referencing)
- Uniques: (`ParentCategoryId`, `Slug`) optionally unique per parent, or global `Slug`
- Indexes: `Slug`, `ParentCategoryId`, `IsActive`
- DeleteBehavior: `Restrict` for children; consider soft-delete cascade policy for subcategories
- Concurrency: optional `RowVersion`

### Product
- PK: `Id (Guid)`
- Properties:
  - `BrandId (Guid?)` FK
  - `Name` (string)
  - `Slug` (string)
  - `Sku` (string)
  - `ShortDescription` (string)
  - `Description` (string)
  - `ProductType` (enum)
  - `Status` (enum)
  - `BasePrice (decimal 18,2)`
  - `CostPrice (decimal 18,2)`
  - `CompareAtPrice (decimal 18,2)`
  - `CurrencyCode` (string)
  - `TaxCategoryId (Guid?)`
  - `Weight (decimal 18,4)`
  - `Length/Width/Height (decimal 18,4)`
  - `IsActive` (bool)
  - `IsFeatured` (bool)
  - `IsDigital` (bool)
  - `RequiresShipping` (bool)
  - `TrackInventory` (bool)
  - `AllowBackorder` (bool)
  - `SeoTitle/SeoDescription/SeoKeywords`
  - `CreatedAt/UpdatedAt/IsDeleted/RowVersion (byte[])`
- FKs: `BrandId` -> `Brand.Id`
- Uniques: `Slug` (unique), `Sku` may be unique globally or per variant
- Indexes: `Slug` (unique), `Sku`, `BrandId`, `Status`, `IsActive`
- DeleteBehavior: Soft-delete (`IsDeleted`); restrict cascade to avoid losing order history
- Concurrency: `RowVersion` (optimistic concurrency)

### ProductVariant
- PK: `Id (Guid)`
- FK: `ProductId (Guid)` -> `Product.Id`
- Properties:
  - `Sku`, `Barcode`, `Name`, `Price (decimal 18,2)`, `CostPrice`, `CompareAtPrice`, `Weight/Length/Width/Height`, `IsActive`, `TrackInventory`, `AllowBackorder`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, `RowVersion`
- Uniques: `Sku` (unique)
- Indexes: `Sku`, `ProductId`, `IsActive`
- DeleteBehavior: Soft-delete; restrict
- Concurrency: `RowVersion` (important for inventory changes)

### ProductImage / ProductVideo
- PK: `Id (Guid)`
- FK: `ProductId (Guid)` and/or `ProductVariantId (Guid?)`
- Properties: `Url`, `AltText`, `IsPrimary`, `SortOrder`, `CreatedAt`
- Indexes: `ProductId`, `ProductVariantId`
- DeleteBehavior: Cascade when product removed? Prefer soft-delete on product and keep media for audit

### Tag / ProductTag
- `Tag` PK Guid, `Name`, `Slug` (unique)
- `ProductTag` linking table: PK composite (ProductId, TagId) or id PK + FKs
- Indexes: (`ProductId`), (`TagId`)
- DeleteBehavior: `Cascade` when tagging removed or `Restrict` depending on soft-delete policy

### ProductCategory / ProductTag
- Linking tables between products and categories/tags. Composite PKs recommended.

---

## Attributes (Product Attributes)

### Attribute
- PK: `Id (Guid)`
- Properties: `Name`, `Code`, `DisplayType`, `IsFilterable`, `IsVariant`, `IsRequired`, `CreatedAt`, `UpdatedAt`
- Unique: `Code`

### AttributeValue
- PK: `Id (Guid)`
- FK: `AttributeId (Guid)` -> `Attribute.Id`
- Properties: `Value`, `SortOrder`

### ProductAttributeValue / ProductVariantAttributeValue
- Linking tables between product/variant and attribute values for filtering and variant generation.
- PK: composite (`ProductId`, `AttributeValueId`) or Guid id + FKs

---

## Inventory

### Warehouse
- PK: `Id (Guid)`
- Properties: `Name`, `Code` (unique), `AddressId` (optional), `IsActive`, `CreatedAt`, `UpdatedAt`
- Unique: `Code`

### InventoryItem
- PK: `Id (Guid)`
- FKs: `ProductId`, `ProductVariantId`, `WarehouseId`
- Properties: `QuantityOnHand` (int), `QuantityReserved` (int), `ReorderLevel` (int), `ReorderQuantity` (int), `AllowBackorder` (bool), `UpdatedAt`, `RowVersion` (byte[])
- Computed AvailableStock: `QuantityOnHand - QuantityReserved`
- Indexes: `WarehouseId`, `ProductId`, `ProductVariantId`
- DeleteBehavior: `Restrict` (do not remove inventory when product soft-deleted)
- Concurrency: `RowVersion` required; optimistic concurrency to prevent oversell

### InventoryTransaction
- PK: `Id (Guid)`
- FKs: `InventoryItemId`, `RelatedEntityId` (OrderId, PurchaseId, TransferId)
- Properties: `Type` (enum), `Quantity`, `BeforeQuantity`, `AfterQuantity`, `Reference`, `CreatedAt`, `Notes`
- Indexes: `InventoryItemId`, `Type`
- DeleteBehavior: `Restrict` (audit trail)

### StockReservation
- PK: `Id (Guid)`
- FKs: `InventoryItemId`, `OrderId (Guid?)`, `CartId (Guid?)`
- Properties: `Quantity`, `Status` (Reserved/Released/Converted/Expired), `ExpiresAt`, `CreatedAt`, `ReleasedAt`
- Indexes: `OrderId`, `CartId`, `InventoryItemId`
- Concurrency: ensure atomic reservation via transactions and InventoryItem RowVersion

### WarehouseTransfer / WarehouseTransferItem
- Transfer metadata and items; FK to source/destination warehouses
- Audit and transactionally consistent updates

---

## Cart & Wishlist

### Cart
- PK: `Id (Guid)`
- FK: `UserId (Guid?)`
- Properties: `SessionId`(string), `CurrencyCode`, `Status` (enum), `CreatedAt`, `UpdatedAt`, `ExpiresAt`
- Indexes: `UserId`, `SessionId`
- DeleteBehavior: Cascade to `CartItem`

### CartItem
- PK: `Id (Guid)`
- FK: `CartId`, `ProductId`, `ProductVariantId`
- Properties: `Quantity`, `UnitPrice`, `CreatedAt`, `UpdatedAt`
- DeleteBehavior: Cascade when cart deleted

### Wishlist / WishlistItem
- Support multiple wishlists per user; unique constraint to prevent duplicate items per wishlist

---

## Orders

### Order
- PK: `Id (Guid)`
- FK: `UserId (Guid?)`
- Properties:
  - `OrderNumber` (string) unique
  - `Status` (enum)
  - `PaymentStatus`, `FulfillmentStatus`
  - `CurrencyCode`
  - `Subtotal`, `DiscountAmount`, `ShippingAmount`, `TaxAmount`, `TotalAmount`, `RefundedAmount` (decimals)
  - `CouponCode` (string)
  - `Notes`, `CustomerNotes`
  - `PlacedAt`, `PaidAt`, `CancelledAt`, `CompletedAt`, `CreatedAt`, `UpdatedAt`
  - `RowVersion` (byte[])
- Uniques: `OrderNumber` (unique)
- Indexes: `UserId`, `OrderNumber`, `Status`, `CreatedAt`
- DeleteBehavior: `Restrict` (historical record)
- Concurrency: `RowVersion` for safe updates

### OrderItem
- PK: `Id (Guid)`
- FK: `OrderId`, `ProductId`, `ProductVariantId`
- Properties: Snapshot fields: `ProductName`, `VariantName`, `Sku`, `UnitPrice`, `Quantity`, `DiscountAmount`, `TaxAmount`, `TotalAmount`, `ProductImageUrl`
- DeleteBehavior: Cascade with Order

### OrderAddress
- PK: `Id (Guid)`
- FK: `OrderId`
- Properties: All address fields as snapshot (immutable)
- DeleteBehavior: Cascade with Order

### OrderStatusHistory
- PK: `Id (Guid)`
- FK: `OrderId`, `ChangedByUserId`
- Properties: `OldStatus`, `NewStatus`, `Comment`, `CreatedAt`
- DeleteBehavior: Cascade with Order

---

## Payments

### Payment
- PK: `Id (Guid)`
- FK: `OrderId` -> `Order.Id`
- Properties: `Provider`, `ProviderPaymentId` (string), `Amount`, `CurrencyCode`, `Status`, `PaymentMethod`, `AuthorizedAt`, `CapturedAt`, `FailedAt`, `FailureReason`, `CreatedAt`, `UpdatedAt`
- Indexes: `ProviderPaymentId` (unique per provider), `OrderId`
- DeleteBehavior: `Restrict` (do not remove for audit/history)

### PaymentTransaction (optional)
- Track captures/authorizations/refunds per payment

### Refund / RefundItem
- PK: `Id (Guid)`
- FK: `PaymentId`, `OrderId`, `OrderItemId`
- Properties: `Amount`, `Reason`, `Status`, `CreatedAt`
- DeleteBehavior: `Restrict`

Security note: never store full card numbers, CVV, PIN, or provider secrets.

---

## Shipping

### ShippingMethod
- PK: `Id (Guid)`
- Properties: `Name`, `Code`, `Carrier`, `IsEnabled`, `SettingsJson`, `CreatedAt`

### Shipment
- PK: `Id (Guid)`
- FK: `OrderId`, `WarehouseId`
- Properties: `TrackingNumber`, `Carrier`, `ShippedAt`, `DeliveredAt`, `Status`, `CreatedAt`

### ShipmentItem
- PK: `Id (Guid)`
- FK: `ShipmentId`, `OrderItemId`, `InventoryItemId`
- Properties: `Quantity`

---

## Coupons & Promotions

### Coupon
- PK: `Id (Guid)`
- Properties: `Code` (string, unique), `Description`, `Type` (percentage/fixed/free-shipping), `Value` (decimal), `StartAt`, `EndAt`, `MinOrderAmount`, `MaxDiscountAmount`, `UsageLimit`, `PerUserLimit`, `CreatedAt`, `IsActive`
- Indexes: `Code`
- DeleteBehavior: `Restrict`

### CouponProduct / CouponCategory / CouponUsage
- Linking tables and usage records (per-user usage counts)

### Promotion
- PK: `Id (Guid)`
- Properties: `Name`, `Type`, `RulesJson`, `StartAt`, `EndAt`, `CreatedAt`, `IsActive`

---

## Tax

### TaxCategory
- PK: `Id (Guid)`
- Properties: `Name`, `Description`

### TaxRate
- PK: `Id (Guid)`
- FK: `TaxCategoryId`
- Properties: `CountryCode`, `RegionCode`, `Rate` (decimal 18,6), `CreatedAt`

---

## Reviews & Q&A

### ProductReview
- PK: `Id (Guid)`
- FK: `ProductId`, `UserId`, `OrderId?` (for verified purchase)
- Properties: `Rating` (int), `Title`, `Comment`, `IsVerifiedPurchase`, `IsApproved`, `CreatedAt`, `UpdatedAt`
- Indexes: `ProductId`, `UserId`

### ReviewImage / ReviewVote
- Associated media and votes

### ProductQuestion / ProductAnswer
- PKs: Guid
- FKs: `ProductId`, `UserId` (asker/answerer)
- Properties: `Question`, `Answer`, `IsOfficial`, `CreatedAt`

---

## Returns

### ReturnRequest
- PK: `Id (Guid)`
- FK: `OrderId`, `UserId`
- Properties: `Status`, `Reason`, `CreatedAt`, `ProcessedAt`

### ReturnItem
- PK: `Id (Guid)`
- FK: `ReturnRequestId`, `OrderItemId`
- Properties: `Quantity`, `Condition`, `RefundAmount`

---

## Notifications

### Notification
- PK: `Id (Guid)`
- FK: `UserId` (optional)
- Properties: `Type` (enum), `DataJson`, `IsRead`, `CreatedAt`

---

## Support / Tickets

### SupportTicket
- PK: `Id (Guid)`
- FK: `UserId`, `AssignedToUserId` (optional)
- Properties: `Subject`, `Status`, `Priority`, `CreatedAt`, `UpdatedAt`

### SupportTicketMessage
- PK: `Id (Guid)`
- FK: `SupportTicketId`, `UserId` (author)
- Properties: `Message`, `IsInternal`, `CreatedAt`

---

## Audit & Logging

### AuditLog
- PK: `Id (Guid)`
- FK: `UserId` (optional)
- Properties: `Action`, `EntityName`, `EntityId`, `OldValues` (json), `NewValues` (json), `IpAddress`, `UserAgent`, `CreatedAt`
- DeleteBehavior: `Restrict` (don't remove historical logs)

---

## Multi-Currency & Localization

### Currency
- PK: `Id (Guid)`
- Properties: `Code` (USD, EUR), `Symbol`, `IsBaseCurrency`

### ExchangeRate
- PK: `Id (Guid)`
- FK: `FromCurrencyId`, `ToCurrencyId`
- Properties: `Rate` (decimal 18,6), `EffectiveAt`

### Language / Translations
- Entities for translations: `ProductTranslation`, `CategoryTranslation`, `BrandTranslation`
- Composite unique: (`ProductId`, `LanguageId`)

---

## Vendor / Marketplace

### Vendor
- PK: `Id (Guid)`
- Properties: `Name`, `Code`, `IsActive`, `CreatedAt`

### VendorProduct
- PK: `Id (Guid)`
- FK: `VendorId`, `ProductId`
- Properties: `VendorSku`, `Price`, `IsActive`

---

## Idempotency

### IdempotencyKey
- PK: `Key` (string) or `Id (Guid)` with `Key` unique
- Properties: `RequestHash`, `OwnerId` (UserId), `Status`, `CreatedAt`, `ExpiresAt`, `ResponseData` (json)
- Used for checkout/payment/refund endpoints

---

## Value Objects (suggested)
- `Money` (Amount, CurrencyCode) — encapsulate operations and validation
- `Address` (use within OrderAddress snapshot and Address entity)
- `Email`, `PhoneNumber`, `Sku`, `OrderNumber` value objects where useful

---

## Global Index & Unique Suggestions Summary
- Product: unique `Slug`, index `Sku`, index `BrandId`, index `Status`, index `IsActive`
- ProductVariant: unique `Sku`, index `Barcode`
- Category: index `Slug` (unique per store)
- Brand: index `Slug`
- Order: unique `OrderNumber`, index `UserId`, index `Status`, index `CreatedAt`
- InventoryItem: indexes on `WarehouseId`, `ProductId`, `ProductVariantId`
- Payment: index `ProviderPaymentId`
- Coupon: unique `Code`

---

## Concurrency and Transaction Boundaries
- Use `RowVersion` (byte[]) for optimistic concurrency on critical mutating entities: `Product`, `ProductVariant`, `InventoryItem`, `Order`.
- Checkout flow must execute within a transaction boundary that:
  - Validates inventory and reserves stock (create `StockReservation` rows)
  - Creates `Order` and `OrderItem` snapshots
  - Creates `Payment` record (pending)
  - Commits transaction and then calls external payment provider (or use outbox/event-driven pattern to finalize capture)
- Inventory reservations and transformations must be ACID per warehouse using DB transactions and `RowVersion` checks to avoid oversell.

---

## Delete Behavior Summary
- Historical records (Orders, Payments, Refunds, AuditLog) — `Restrict` (never cascade delete)
- Master records (Product, Category, Brand) — soft-delete (`IsDeleted`) with global query filters
- Aggregates that are logically owned (Cart -> CartItems, Order -> OrderItems, Order -> OrderAddress) — cascade-delete allowed
- User deletion: do not cascade delete Orders, Payments, Refunds, AuditLogs; mark user inactive instead

---

This completes the first pass of the entity/property listing. Next: produce a per-entity detailed constraints table (PK types, FK references, unique constraints and DDL-style index suggestions) and then map domain rules to entities and application use cases.
