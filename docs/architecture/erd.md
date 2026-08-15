# Entity-Relationship Diagram (High-level)

This ERD captures the primary entities and relationships for the E-Commerce backend. It's intentionally high-level; detailed property lists, indexes, and constraints will follow.

```mermaid
erDiagram
    APPLICATIONUSER {
        GUID Id PK
        string Email
    }
    USERPROFILE {
        GUID Id PK
        GUID UserId FK
        string FirstName
        string LastName
    }
    ADDRESS {
        GUID Id PK
        GUID UserId FK
        string AddressLine1
        string City
    }
    PRODUCT {
        GUID Id PK
        GUID BrandId FK
        string Name
        decimal BasePrice
    }
    PRODUCTVARIANT {
        GUID Id PK
        GUID ProductId FK
        string Sku
        decimal Price
    }
    CATEGORY {
        GUID Id PK
        GUID ParentCategoryId FK
        string Name
    }
    WAREHOUSE {
        GUID Id PK
        string Name
    }
    INVENTORYITEM {
        GUID Id PK
        GUID ProductId FK
        GUID ProductVariantId FK
        GUID WarehouseId FK
        int QuantityOnHand
    }
    CART {
        GUID Id PK
        GUID UserId FK
    }
    CARTITEM {
        GUID Id PK
        GUID CartId FK
        GUID ProductVariantId FK
        int Quantity
    }
    ORDER {
        GUID Id PK
        GUID UserId FK
        string OrderNumber
        decimal TotalAmount
    }
    ORDERITEM {
        GUID Id PK
        GUID OrderId FK
        GUID ProductVariantId FK
        decimal UnitPrice
        int Quantity
    }
    PAYMENT {
        GUID Id PK
        GUID OrderId FK
        string Provider
        decimal Amount
    }

    APPLICATIONUSER ||--o{ USERPROFILE : has
    APPLICATIONUSER ||--o{ ADDRESS : has
    PRODUCT ||--o{ PRODUCTVARIANT : has
    PRODUCT ||--o{ PRODUCT : categorizes
    CATEGORY ||--o{ PRODUCT : contains
    PRODUCTVARIANT ||--o{ INVENTORYITEM : stocked_in
    WAREHOUSE ||--o{ INVENTORYITEM : stores
    CART ||--o{ CARTITEM : contains
    PRODUCTVARIANT ||--o{ CARTITEM : referenced_by
    ORDER ||--o{ ORDERITEM : contains
    PRODUCTVARIANT ||--o{ ORDERITEM : referenced_by
    ORDER ||--o{ PAYMENT : has
```

Notes:
- This ERD is a starting point; many domain tables (Coupons, Promotions, Reviews, Shipments, Returns, AuditLogs, Translations, Vendors, etc.) will be added in the detailed model.
- Next: expand each entity with full property lists, PKs, FKs, unique constraints, indexes, delete behaviors, and concurrency controls.
