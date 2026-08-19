# API Documentation

Base URL: `https://localhost:7001` (development)  
Authentication: JWT Bearer token (`Authorization: Bearer <token>`)  
Content-Type: `application/json`

---

## Authentication

### Register
**POST** `/api/account/register`

**Request Body:**
```json
{
  "email": "string (required)",
  "password": "string (required)"
}
```

**Response:** `200 OK`
```json
{
  "message": "Registration successful. Verification email sent."
}
```

**Side Effect:** Creates user with `IsActive=true`, `CreatedAt=now`, `UpdatedAt=now`. Assigns "Customer" role.

---

### Login
**POST** `/api/account/login`

**Request Body:**
```json
{
  "email": "string (required)",
  "password": "string (required)"
}
```

**Response:** `200 OK`
```json
{
  "token": "string",
  "refreshToken": "string",
  "refreshTokenExpires": "2026-08-18T00:00:00Z"
}
```

**Error Responses:**
| Status | Description |
|--------|-------------|
| `401` | Invalid credentials |
| `401` | Email not verified |
| `401` | Account is deactivated |

**Side Effect:** Updates `LastLoginAt` timestamp on successful login.

---

### Verify Email
**POST** `/api/account/verify-email`

**Request Body:**
```json
{
  "email": "string (required)",
  "token": "string (required)"
}
```

**Response:** `200 OK`
```json
{
  "message": "Email verified successfully."
}
```

---

### Resend Verification Email
**POST** `/api/account/resend-verification`

**Request Body:**
```json
{
  "email": "string (required)"
}
```

**Response:** `200 OK`
```json
{
  "message": "Verification email sent."
}
```

---

### Forgot Password
**POST** `/api/account/forgot-password`

**Request Body:**
```json
{
  "email": "string (required)"
}
```

**Response:** `200 OK`
```json
{
  "message": "Password reset email sent."
}
```

---

### Reset Password
**POST** `/api/account/reset-password`

**Request Body:**
```json
{
  "email": "string (required)",
  "token": "string (required)",
  "newPassword": "string (required)"
}
```

**Response:** `200 OK`
```json
{
  "message": "Password reset successfully."
}
```

---

### Refresh Token
**POST** `/api/account/refresh`

**Request Body:**
```json
{
  "refreshToken": "string (required)"
}
```

**Response:** `200 OK`
```json
{
  "token": "string",
  "refreshToken": "string",
  "refreshTokenExpires": "2026-08-18T00:00:00Z"
}
```

---

### Revoke Refresh Token
**POST** `/api/account/revoke`

**Headers:** `Authorization: Bearer <token>`

**Request Body:**
```json
{
  "refreshToken": "string (required)"
}
```

**Response:** `204 No Content`

---

### Revoke All Refresh Tokens
**POST** `/api/account/revoke-all`

**Headers:** `Authorization: Bearer <token>`

**Response:** `204 No Content`

---

### Get Current User
**GET** `/api/account/me`

**Headers:** `Authorization: Bearer <token>`

**Response:** `200 OK`
```json
{
  "id": "guid",
  "email": "string",
  "userName": "string",
  "roles": ["string"]
}
```

---

## Products

### Get Products
**GET** `/api/products`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| search | string | No | Search term |
| categoryId | guid | No | Filter by category |
| brandId | guid | No | Filter by brand |
| minPrice | decimal | No | Minimum price |
| maxPrice | decimal | No | Maximum price |
| isActive | bool | No | Filter by active status |
| sortBy | string | No | Sort by: name, price_asc, price_desc, newest, featured |

**Response:** `200 OK`
```json
[
  {
    "id": "guid",
    "name": "string",
    "slug": "string",
    "description": "string",
    "price": "decimal",
    "categoryId": "guid",
    "brandId": "guid",
    "isActive": true,
    "createdAt": "2026-08-18T00:00:00Z",
    "images": [
      {
        "id": "guid",
        "productId": "guid",
        "productVariantId": "guid?",
        "url": "string",
        "altText": "string",
        "isPrimary": true,
        "sortOrder": 0,
        "createdAt": "2026-08-18T00:00:00Z"
      }
    ]
  }
]
```

---

### Search Products
**GET** `/api/products/search`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| q | string | Yes | Search term |
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |

**Response:** `200 OK`
```json
[
  {
    "id": "guid",
    "name": "string",
    "slug": "string",
    "description": "string",
    "price": "decimal",
    "categoryId": "guid",
    "brandId": "guid",
    "isActive": true,
    "createdAt": "2026-08-18T00:00:00Z",
    "images": [
      {
        "id": "guid",
        "productId": "guid",
        "productVariantId": "guid?",
        "url": "string",
        "altText": "string",
        "isPrimary": true,
        "sortOrder": 0,
        "createdAt": "2026-08-18T00:00:00Z"
      }
    ]
  }
]
```

---

### Get Product By ID
**GET** `/api/products/{id:guid}`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Product ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "slug": "string",
  "description": "string",
  "price": "decimal",
  "categoryId": "guid",
  "brandId": "guid",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z",
  "images": [
    {
      "id": "guid",
      "productId": "guid",
      "productVariantId": "guid?",
      "url": "string",
      "altText": "string",
      "isPrimary": true,
      "sortOrder": 0,
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ]
}
```

---

### Get Product By Slug
**GET** `/api/products/slug/{slug}`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| slug | string | Product slug |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "slug": "string",
  "description": "string",
  "price": "decimal",
  "categoryId": "guid",
  "brandId": "guid",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z",
  "images": [
    {
      "id": "guid",
      "productId": "guid",
      "productVariantId": "guid?",
      "url": "string",
      "altText": "string",
      "isPrimary": true,
      "sortOrder": 0,
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ]
}
```

---

## Cart

**All endpoints require authentication.**

### Get Cart
**GET** `/api/cart`

**Headers:** `Authorization: Bearer <token>`

**Response:** `200 OK`
```json
{
  "id": "guid",
  "userId": "guid",
  "status": "Active",
  "items": [
    {
      "id": "guid",
      "productId": "guid",
      "productVariantId": "guid?",
      "quantity": 1,
      "unitPrice": "decimal",
      "productName": "string",
      "productSlug": "string",
      "variantName": "string?",
      "imageUrl": "string?"
    }
  ],
  "subtotal": "decimal",
  "discount": "decimal",
  "tax": "decimal",
  "total": "decimal",
  "createdAt": "2026-08-18T00:00:00Z",
  "updatedAt": "2026-08-18T00:00:00Z"
}
```

---

### Add Item to Cart
**POST** `/api/cart/items`

**Headers:** `Authorization: Bearer <token>`

**Request Body:**
```json
{
  "productId": "guid (required)",
  "productVariantId": "guid? (optional)",
  "quantity": 1
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "userId": "guid",
  "status": "Active",
  "items": [...],
  "subtotal": "decimal",
  "discount": "decimal",
  "tax": "decimal",
  "total": "decimal"
}
```

---

### Update Cart Item
**PUT** `/api/cart/items/{itemId:guid}`

**Headers:** `Authorization: Bearer <token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| itemId | guid | Cart item ID |

**Request Body:**
```json
{
  "quantity": 1
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "userId": "guid",
  "status": "Active",
  "items": [...],
  "subtotal": "decimal",
  "discount": "decimal",
  "tax": "decimal",
  "total": "decimal"
}
```

---

### Remove Cart Item
**DELETE** `/api/cart/items/{itemId:guid}`

**Headers:** `Authorization: Bearer <token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| itemId | guid | Cart item ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "userId": "guid",
  "status": "Active",
  "items": [...],
  "subtotal": "decimal",
  "discount": "decimal",
  "tax": "decimal",
  "total": "decimal"
}
```

---

### Clear Cart
**DELETE** `/api/cart`

**Headers:** `Authorization: Bearer <token>`

**Response:** `200 OK`
```json
{
  "id": "guid",
  "userId": "guid",
  "status": "Active",
  "items": [],
  "subtotal": 0,
  "discount": 0,
  "tax": 0,
  "total": 0
}
```

---

## Orders

### Get Orders
**GET** `/api/orders`

**Headers:** `Authorization: Bearer <token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |

**Response:** `200 OK`
```json
[
  {
    "id": "guid",
    "orderNumber": "string",
    "userId": "guid",
    "status": "Placed",
    "paymentStatus": "Pending",
    "fulfillmentStatus": "Pending",
    "subtotal": "decimal",
    "discount": "decimal",
    "shipping": "decimal",
    "tax": "decimal",
    "total": "decimal",
    "items": [...],
    "createdAt": "2026-08-18T00:00:00Z"
  }
]
```

---

### Get Order By ID
**GET** `/api/orders/{id:guid}`

**Headers:** `Authorization: Bearer <token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Order ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "orderNumber": "string",
  "userId": "guid",
  "status": "Placed",
  "paymentStatus": "Pending",
  "fulfillmentStatus": "Pending",
  "subtotal": "decimal",
  "discount": "decimal",
  "shipping": "decimal",
  "tax": "decimal",
  "total": "decimal",
  "items": [...],
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Mark Order as Paid
**POST** `/api/orders/{id:guid}/pay`

**Headers:** `Authorization: Bearer <token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Order ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "orderNumber": "string",
  "userId": "guid",
  "status": "Paid",
  "paymentStatus": "Paid",
  "fulfillmentStatus": "Pending",
  "subtotal": "decimal",
  "discount": "decimal",
  "shipping": "decimal",
  "tax": "decimal",
  "total": "decimal",
  "items": [...],
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Complete Order
**POST** `/api/orders/{id:guid}/complete`

**Headers:** `Authorization: Bearer <token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Order ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "orderNumber": "string",
  "userId": "guid",
  "status": "Completed",
  "paymentStatus": "Paid",
  "fulfillmentStatus": "Delivered",
  "subtotal": "decimal",
  "discount": "decimal",
  "shipping": "decimal",
  "tax": "decimal",
  "total": "decimal",
  "items": [...],
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Cancel Order
**POST** `/api/orders/{id:guid}/cancel`

**Headers:** `Authorization: Bearer <token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Order ID |

**Request Body:**
```json
{
  "reason": "string? (optional)"
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "orderNumber": "string",
  "userId": "guid",
  "status": "Cancelled",
  "paymentStatus": "Refunded",
  "fulfillmentStatus": "Cancelled",
  "subtotal": "decimal",
  "discount": "decimal",
  "shipping": "decimal",
  "tax": "decimal",
  "total": "decimal",
  "items": [...],
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Get Order Shipment
**GET** `/api/orders/{id:guid}/shipment`

**Headers:** `Authorization: Bearer <token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Order ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "orderId": "guid",
  "status": "Shipped",
  "carrier": "string",
  "trackingNumber": "string",
  "shippedAt": "2026-08-18T00:00:00Z",
  "deliveredAt": "2026-08-18T00:00:00Z"
}
```

---

## Checkout

### Checkout
**POST** `/api/checkout`

**Headers:** `Authorization: Bearer <token>`

**Request Body:**
```json
{
  "shippingAddressId": "guid (required)",
  "billingAddressId": "guid (required)",
  "shippingMethodId": "guid (required)",
  "couponCode": "string? (optional)",
  "idempotencyKey": "string (required)"
}
```

**Response:** `202 Accepted`
```json
{
  "orderId": "guid"
}
```

---

## Addresses

**All endpoints require authentication (AdminOrCustomer policy).**

### Get My Addresses
**GET** `/api/addresses`

**Headers:** `Authorization: Bearer <token>`

**Response:** `200 OK`
```json
[
  {
    "id": "guid",
    "userId": "guid",
    "addressLine1": "string",
    "addressLine2": "string?",
    "city": "string",
    "state": "string",
    "postalCode": "string",
    "country": "string",
    "isDefault": true
  }
]
```

---

### Get Address By ID
**GET** `/api/addresses/{id:guid}`

**Headers:** `Authorization: Bearer <token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Address ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "userId": "guid",
  "addressLine1": "string",
  "addressLine2": "string?",
  "city": "string",
  "state": "string",
  "postalCode": "string",
  "country": "string",
  "isDefault": true
}
```

---

### Create Address
**POST** `/api/addresses`

**Headers:** `Authorization: Bearer <token>`

**Request Body:**
```json
{
  "addressLine1": "string (required)",
  "addressLine2": "string?",
  "city": "string (required)",
  "state": "string (required)",
  "postalCode": "string (required)",
  "country": "string (required)",
  "isDefault": false
}
```

**Response:** `201 Created`
```json
{
  "id": "guid",
  "userId": "guid",
  "addressLine1": "string",
  "addressLine2": "string?",
  "city": "string",
  "state": "string",
  "postalCode": "string",
  "country": "string",
  "isDefault": false
}
```

---

### Update Address
**PUT** `/api/addresses/{id:guid}`

**Headers:** `Authorization: Bearer <token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Address ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "addressLine1": "string (required)",
  "addressLine2": "string?",
  "city": "string (required)",
  "state": "string (required)",
  "postalCode": "string (required)",
  "country": "string (required)",
  "isDefault": false
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "userId": "guid",
  "addressLine1": "string",
  "addressLine2": "string?",
  "city": "string",
  "state": "string",
  "postalCode": "string",
  "country": "string",
  "isDefault": false
}
```

---

### Delete Address
**DELETE** `/api/addresses/{id:guid}`

**Headers:** `Authorization: Bearer <token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Address ID |

**Response:** `204 No Content`

---

## Reviews

**All endpoints require authentication (AdminOrCustomer policy).**

### Get Product Reviews
**GET** `/api/products/{productId:guid}/reviews`

**Headers:** `Authorization: Bearer <token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| productId | guid | Product ID |

**Response:** `200 OK`
```json
[
  {
    "id": "guid",
    "productId": "guid",
    "userId": "guid",
    "rating": 5,
    "title": "string",
    "comment": "string",
    "isApproved": true,
    "createdAt": "2026-08-18T00:00:00Z"
  }
]
```

---

### Submit Review
**POST** `/api/products/{productId:guid}/reviews`

**Headers:** `Authorization: Bearer <token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| productId | guid | Product ID |

**Request Body:**
```json
{
  "productId": "guid (required)",
  "rating": 5,
  "title": "string",
  "comment": "string"
}
```

**Response:** `201 Created`
```json
{
  "id": "guid",
  "productId": "guid",
  "userId": "guid",
  "rating": 5,
  "title": "string",
  "comment": "string",
  "isApproved": false,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

## Profile

**All endpoints require authentication (AdminOrCustomer policy).**

### Get My Profile
**GET** `/api/profile`

**Headers:** `Authorization: Bearer <token>`

**Response:** `200 OK`
```json
{
  "id": "guid",
  "email": "string",
  "userName": "string",
  "firstName": "string",
  "lastName": "string",
  "displayName": "string",
  "profileImageUrl": "string",
  "gender": "string",
  "dateOfBirth": "2026-08-18T00:00:00Z",
  "phoneNumber": "string",
  "isActive": true,
  "isEmailVerified": true,
  "isPhoneVerified": false,
  "createdAt": "2026-08-18T00:00:00Z",
  "lastLoginAt": "2026-08-18T00:00:00Z",
  "roles": ["Customer"]
}
```

---

### Update My Profile
**PUT** `/api/profile`

**Headers:** `Authorization: Bearer <token>`

**Request Body:**
```json
{
  "firstName": "string",
  "lastName": "string",
  "displayName": "string",
  "profileImageUrl": "string",
  "gender": "string",
  "dateOfBirth": "2026-08-18T00:00:00Z",
  "phoneNumber": "string"
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "email": "string",
  "userName": "string",
  "firstName": "string",
  "lastName": "string",
  "displayName": "string",
  "profileImageUrl": "string",
  "gender": "string",
  "dateOfBirth": "2026-08-18T00:00:00Z",
  "phoneNumber": "string",
  "isActive": true,
  "isEmailVerified": true,
  "isPhoneVerified": false,
  "createdAt": "2026-08-18T00:00:00Z",
  "lastLoginAt": "2026-08-18T00:00:00Z",
  "roles": ["Customer"]
}
```

---

## Tags

### Get All Tags
**GET** `/api/tags`

**Response:** `200 OK`
```json
[
  {
    "id": "guid",
    "name": "string",
    "slug": "string",
    "createdAt": "2026-08-18T00:00:00Z"
  }
]
```

---

## Coupons

**All endpoints require authentication.**

### Validate Coupon
**POST** `/api/coupons/validate`

**Headers:** `Authorization: Bearer <token>`

**Request Body:**
```json
{
  "code": "string (required)",
  "userId": "guid (required)",
  "orderTotal": "decimal (required)",
  "productIds": ["guid"],
  "categoryIds": ["guid"]
}
```

**Response:** `200 OK`
```json
{
  "isValid": true,
  "couponId": "guid",
  "code": "string",
  "discountType": "Percentage",
  "discountValue": "decimal",
  "discountAmount": "decimal"
}
```

---

### Calculate Discounts
**POST** `/api/coupons/calculate`

**Headers:** `Authorization: Bearer <token>`

**Request Body:**
```json
{
  "code": "string (required)",
  "orderTotal": "decimal (required)",
  "productIds": ["guid"],
  "categoryIds": ["guid"]
}
```

**Response:** `200 OK`
```json
{
  "discountAmount": "decimal",
  "finalTotal": "decimal"
}
```

---

## Currencies

### Get All Currencies
**GET** `/api/currencies`

**Response:** `200 OK`
```json
[
  {
    "id": "guid",
    "code": "USD",
    "name": "US Dollar",
    "symbol": "$",
    "isBaseCurrency": true,
    "isActive": true,
    "createdAt": "2026-08-18T00:00:00Z"
  }
]
```

---

### Get Exchange Rates
**GET** `/api/currencies/rates`

**Response:** `200 OK`
```json
[
  {
    "id": "guid",
    "fromCurrencyId": "guid",
    "toCurrencyId": "guid",
    "fromCurrencyCode": "USD",
    "toCurrencyCode": "EUR",
    "rate": "decimal",
    "effectiveDate": "2026-08-18T00:00:00Z",
    "createdAt": "2026-08-18T00:00:00Z"
  }
]
```

---

### Convert Currency
**GET** `/api/currencies/convert`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| amount | decimal | Yes | Amount to convert |
| from | string | No | From currency code (default: USD) |
| to | string | No | To currency code (default: EUR) |

**Response:** `200 OK`
```json
{
  "amount": "decimal",
  "fromCurrency": "USD",
  "toCurrency": "EUR",
  "convertedAmount": "decimal",
  "rate": "decimal"
}
```

---

## Support Tickets

**All endpoints require authentication (AdminOrCustomer policy).**

### Get My Support Tickets
**GET** `/api/support-tickets`

**Headers:** `Authorization: Bearer <token>`

**Response:** `200 OK`
```json
[
  {
    "id": "guid",
    "userId": "guid",
    "subject": "string",
    "status": "Open",
    "priority": "Medium",
    "createdAt": "2026-08-18T00:00:00Z"
  }
]
```

---

### Get Support Ticket By ID
**GET** `/api/support-tickets/{id:guid}`

**Headers:** `Authorization: Bearer <token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Support ticket ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "userId": "guid",
  "subject": "string",
  "status": "Open",
  "priority": "Medium",
  "messages": [
    {
      "id": "guid",
      "ticketId": "guid",
      "userId": "guid",
      "message": "string",
      "isFromAdmin": false,
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Create Support Ticket
**POST** `/api/support-tickets`

**Headers:** `Authorization: Bearer <token>`

**Request Body:**
```json
{
  "subject": "string (required)",
  "message": "string (required)",
  "priority": "Medium"
}
```

**Response:** `201 Created`
```json
{
  "id": "guid",
  "userId": "guid",
  "subject": "string",
  "status": "Open",
  "priority": "Medium",
  "messages": [...],
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Reply to Support Ticket
**POST** `/api/support-tickets/{id:guid}/reply`

**Headers:** `Authorization: Bearer <token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Support ticket ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "message": "string (required)"
}
```

**Response:** `204 No Content`

---

## Categories

### Get All Categories
**GET** `/api/categories`

**Response:** `200 OK`
```json
[
  {
    "id": "guid",
    "name": "string",
    "slug": "string",
    "description": "string",
    "parentCategoryId": "guid?",
    "imageUrl": "string?",
    "isActive": true,
    "createdAt": "2026-08-18T00:00:00Z"
  }
]
```

---

### Get Category By Slug
**GET** `/api/categories/{slug}`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| slug | string | Category slug |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "slug": "string",
  "description": "string",
  "parentCategoryId": "guid?",
  "imageUrl": "string?",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

## Brands

### Get All Brands
**GET** `/api/brands`

**Response:** `200 OK`
```json
[
  {
    "id": "guid",
    "name": "string",
    "slug": "string",
    "logoUrl": "string?",
    "description": "string?",
    "isActive": true,
    "createdAt": "2026-08-18T00:00:00Z"
  }
]
```

---

## Stripe Webhook

### Handle Stripe Webhook
**POST** `/api/stripe/webhook`

**Headers:**
- `Stripe-Signature`: Stripe webhook signature

**Body:** Raw JSON from Stripe

**Response:** `200 OK`
```json
{
  "handled": true,
  "eventType": "payment_intent.succeeded",
  "message": "Webhook processed successfully"
}
```

---

## Admin: Dashboard

**Requires AdminOnly policy.**

### Get Dashboard Metrics
**GET** `/api/admin/dashboard`

**Headers:** `Authorization: Bearer <admin_token>`

**Response:** `200 OK`
```json
{
  "totalRevenue": "decimal",
  "totalOrders": 100,
  "totalProducts": 50,
  "totalUsers": 200,
  "recentOrders": [...],
  "topProducts": [...],
  "salesByDate": [...]
}
```

---

## Admin: Products

**Requires AdminOnly policy.**

### Get All Products (Admin)
**GET** `/api/admin/products`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| search | string | No | Search term |
| status | string | No | Filter by status |
| brandId | guid | No | Filter by brand |
| isActive | bool | No | Filter by active status |
| includeDeleted | bool | No | Include soft-deleted (default: false) |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "name": "string",
      "slug": "string",
      "description": "string",
      "price": "decimal",
      "categoryId": "guid",
      "brandId": "guid",
      "isActive": true,
      "isDeleted": false,
      "createdAt": "2026-08-18T00:00:00Z",
      "updatedAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Get Product By ID (Admin)
**GET** `/api/admin/products/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Product ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "slug": "string",
  "description": "string",
  "price": "decimal",
  "categoryId": "guid",
  "brandId": "guid",
  "isActive": true,
  "isDeleted": false,
  "createdAt": "2026-08-18T00:00:00Z",
  "updatedAt": "2026-08-18T00:00:00Z"
}
```

---

### Create Product
**POST** `/api/admin/products`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "name": "string (required)",
  "slug": "string (required)",
  "description": "string",
  "price": "decimal (required)",
  "categoryId": "guid (required)",
  "brandId": "guid (required)",
  "isActive": true
}
```

**Response:** `201 Created`
```json
{
  "id": "guid",
  "name": "string",
  "slug": "string",
  "description": "string",
  "price": "decimal",
  "categoryId": "guid",
  "brandId": "guid",
  "isActive": true,
  "isDeleted": false,
  "createdAt": "2026-08-18T00:00:00Z",
  "updatedAt": "2026-08-18T00:00:00Z"
}
```

---

### Update Product
**PUT** `/api/admin/products/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Product ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "name": "string",
  "slug": "string",
  "description": "string",
  "price": "decimal",
  "categoryId": "guid",
  "brandId": "guid",
  "isActive": true
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "slug": "string",
  "description": "string",
  "price": "decimal",
  "categoryId": "guid",
  "brandId": "guid",
  "isActive": true,
  "isDeleted": false,
  "createdAt": "2026-08-18T00:00:00Z",
  "updatedAt": "2026-08-18T00:00:00Z"
}
```

---

### Delete Product
**DELETE** `/api/admin/products/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Product ID |

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| hardDelete | bool | No | Permanently delete (default: false) |

**Response:** `204 No Content`

---

## Admin: Orders

**Requires AdminOnly policy.**

### Get All Orders (Admin)
**GET** `/api/admin/orders`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| search | string | No | Search term |
| status | string | No | Filter by order status |
| paymentStatus | string | No | Filter by payment status |
| fulfillmentStatus | string | No | Filter by fulfillment status |
| userId | guid | No | Filter by user |
| fromDate | datetime | No | Filter from date |
| toDate | datetime | No | Filter to date |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "orderNumber": "string",
      "userId": "guid",
      "status": "Placed",
      "paymentStatus": "Pending",
      "fulfillmentStatus": "Pending",
      "subtotal": "decimal",
      "discount": "decimal",
      "shipping": "decimal",
      "tax": "decimal",
      "total": "decimal",
      "items": [...],
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Get Order By ID (Admin)
**GET** `/api/admin/orders/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Order ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "orderNumber": "string",
  "userId": "guid",
  "status": "Placed",
  "paymentStatus": "Pending",
  "fulfillmentStatus": "Pending",
  "subtotal": "decimal",
  "discount": "decimal",
  "shipping": "decimal",
  "tax": "decimal",
  "total": "decimal",
  "items": [...],
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Ship Order
**POST** `/api/admin/orders/{id:guid}/ship`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Order ID |

**Request Body:**
```json
{
  "orderId": "guid (required)",
  "carrier": "string (required)",
  "trackingNumber": "string (required)",
  "shippedAt": "2026-08-18T00:00:00Z"
}
```

**Response:** `204 No Content`

---

### Mark Order as Delivered
**POST** `/api/admin/orders/{id:guid}/deliver`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Order ID |

**Response:** `204 No Content`

---

### Refund Order
**POST** `/api/admin/orders/{id:guid}/refund`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Order ID |

**Request Body:**
```json
{
  "orderId": "guid (required)",
  "amount": "decimal (required)",
  "reason": "string"
}
```

**Response:** `204 No Content`

---

### Process Order Return
**POST** `/api/admin/orders/{id:guid}/return`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Order ID |

**Request Body:**
```json
{
  "orderId": "guid (required)",
  "items": [
    {
      "orderItemId": "guid (required)",
      "quantity": 1,
      "reason": "string"
    }
  ]
}
```

**Response:** `204 No Content`

---

## Admin: Users

**Requires AdminOnly policy.**

### Get All Users (Admin)
**GET** `/api/admin/users`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| search | string | No | Search term |
| role | string | No | Filter by role |
| isActive | bool | No | Filter by active status |
| includeDeleted | bool | No | Include soft-deleted (default: false) |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "email": "string",
      "userName": "string",
      "firstName": "string",
      "lastName": "string",
      "displayName": "string",
      "profileImageUrl": "string",
      "gender": "string",
      "dateOfBirth": "2026-08-18T00:00:00Z",
      "phoneNumber": "string",
      "isActive": true,
      "isEmailVerified": true,
      "isPhoneVerified": false,
      "createdAt": "2026-08-18T00:00:00Z",
      "lastLoginAt": "2026-08-18T00:00:00Z",
      "roles": ["Customer"]
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Get User By ID (Admin)
**GET** `/api/admin/users/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | User ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "email": "string",
  "userName": "string",
  "firstName": "string",
  "lastName": "string",
  "displayName": "string",
  "profileImageUrl": "string",
  "gender": "string",
  "dateOfBirth": "2026-08-18T00:00:00Z",
  "phoneNumber": "string",
  "isActive": true,
  "isEmailVerified": true,
  "isPhoneVerified": false,
  "createdAt": "2026-08-18T00:00:00Z",
  "lastLoginAt": "2026-08-18T00:00:00Z",
  "roles": ["Customer"]
}
```

---

### Create User
**POST** `/api/admin/users`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "email": "string (required)",
  "userName": "string (required)",
  "password": "string (required)",
  "firstName": "string",
  "lastName": "string",
  "displayName": "string",
  "profileImageUrl": "string",
  "gender": "string",
  "dateOfBirth": "2026-08-18T00:00:00Z",
  "phoneNumber": "string",
  "isActive": true,
  "isEmailVerified": false,
  "isPhoneVerified": false,
  "roles": ["string"]
}
```

**Response:** `201 Created`
```json
{
  "id": "guid",
  "email": "string",
  "userName": "string",
  "firstName": "string",
  "lastName": "string",
  "displayName": "string",
  "profileImageUrl": "string",
  "gender": "string",
  "dateOfBirth": "2026-08-18T00:00:00Z",
  "phoneNumber": "string",
  "isActive": true,
  "isEmailVerified": true,
  "isPhoneVerified": false,
  "createdAt": "2026-08-18T00:00:00Z",
  "lastLoginAt": "2026-08-18T00:00:00Z",
  "roles": ["Customer"]
}
```

---

### Update User
**PUT** `/api/admin/users/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | User ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "email": "string",
  "userName": "string",
  "firstName": "string",
  "lastName": "string",
  "displayName": "string",
  "profileImageUrl": "string",
  "gender": "string",
  "dateOfBirth": "2026-08-18T00:00:00Z",
  "phoneNumber": "string",
  "isActive": true,
  "isEmailVerified": true,
  "isPhoneVerified": false,
  "roles": ["string"]
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "email": "string",
  "userName": "string",
  "firstName": "string",
  "lastName": "string",
  "isActive": true,
  "isDeleted": false,
  "roles": ["string"],
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Delete User
**DELETE** `/api/admin/users/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | User ID |

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| hardDelete | bool | No | Permanently delete (default: false) |

**Response:** `204 No Content`

---

### Change User Password
**POST** `/api/admin/users/{id:guid}/change-password`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | User ID |

**Request Body:**
```json
{
  "userId": "guid (required)",
  "newPassword": "string (required)"
}
```

**Response:** `204 No Content`

---

### Set User Roles
**POST** `/api/admin/users/{id:guid}/roles`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | User ID |

**Request Body:**
```json
{
  "userId": "guid (required)",
  "roles": ["string (required)"]
}
```

**Response:** `204 No Content`

---

## Admin: Coupons

**Requires AdminOnly policy.**

### Get All Coupons (Admin)
**GET** `/api/admin/coupons`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| search | string | No | Search term |
| isActive | bool | No | Filter by active status |
| type | string | No | Filter by type |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "code": "string",
      "description": "string",
      "discountType": "Percentage",
      "discountValue": "decimal",
      "minOrderAmount": "decimal",
      "maxDiscountAmount": "decimal",
      "usageLimit": 100,
      "usageCount": 50,
      "isActive": true,
      "startsAt": "2026-08-18T00:00:00Z",
      "expiresAt": "2026-08-18T00:00:00Z",
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Get Coupon By ID (Admin)
**GET** `/api/admin/coupons/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Coupon ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "code": "string",
  "description": "string",
  "discountType": "Percentage",
  "discountValue": "decimal",
  "minOrderAmount": "decimal",
  "maxDiscountAmount": "decimal",
  "usageLimit": 100,
  "usageCount": 50,
  "isActive": true,
  "startsAt": "2026-08-18T00:00:00Z",
  "expiresAt": "2026-08-18T00:00:00Z",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Get Coupon By Code (Admin)
**GET** `/api/admin/coupons/by-code/{code}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| code | string | Coupon code |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "code": "string",
  "description": "string",
  "discountType": "Percentage",
  "discountValue": "decimal",
  "minOrderAmount": "decimal",
  "maxDiscountAmount": "decimal",
  "usageLimit": 100,
  "usageCount": 50,
  "isActive": true,
  "startsAt": "2026-08-18T00:00:00Z",
  "expiresAt": "2026-08-18T00:00:00Z",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Create Coupon
**POST** `/api/admin/coupons`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "code": "string (required)",
  "description": "string",
  "discountType": "Percentage",
  "discountValue": "decimal (required)",
  "minOrderAmount": "decimal",
  "maxDiscountAmount": "decimal",
  "usageLimit": 100,
  "isActive": true,
  "startsAt": "2026-08-18T00:00:00Z",
  "expiresAt": "2026-08-18T00:00:00Z"
}
```

**Response:** `201 Created`
```json
{
  "id": "guid",
  "code": "string",
  "description": "string",
  "discountType": "Percentage",
  "discountValue": "decimal",
  "minOrderAmount": "decimal",
  "maxDiscountAmount": "decimal",
  "usageLimit": 100,
  "usageCount": 0,
  "isActive": true,
  "startsAt": "2026-08-18T00:00:00Z",
  "expiresAt": "2026-08-18T00:00:00Z",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Update Coupon
**PUT** `/api/admin/coupons/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Coupon ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "code": "string",
  "description": "string",
  "discountType": "Percentage",
  "discountValue": "decimal",
  "minOrderAmount": "decimal",
  "maxDiscountAmount": "decimal",
  "usageLimit": 100,
  "isActive": true,
  "startsAt": "2026-08-18T00:00:00Z",
  "expiresAt": "2026-08-18T00:00:00Z"
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "code": "string",
  "description": "string",
  "discountType": "Percentage",
  "discountValue": "decimal",
  "minOrderAmount": "decimal",
  "maxDiscountAmount": "decimal",
  "usageLimit": 100,
  "usageCount": 50,
  "isActive": true,
  "startsAt": "2026-08-18T00:00:00Z",
  "expiresAt": "2026-08-18T00:00:00Z",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Delete Coupon
**DELETE** `/api/admin/coupons/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Coupon ID |

**Response:** `204 No Content`

---

### Validate Coupon (Admin)
**POST** `/api/admin/coupons/validate`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "code": "string (required)",
  "userId": "guid (required)",
  "orderTotal": "decimal (required)",
  "productIds": ["guid"],
  "categoryIds": ["guid"]
}
```

**Response:** `200 OK`
```json
{
  "isValid": true,
  "couponId": "guid",
  "code": "string",
  "discountType": "Percentage",
  "discountValue": "decimal",
  "discountAmount": "decimal"
}
```

---

## Admin: Product Variants

**Requires AdminOnly policy.**

### Get Product Variants
**GET** `/api/admin/products/{productId:guid}/variants`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| productId | guid | Product ID |

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| search | string | No | Search term |
| isActive | bool | No | Filter by active status |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "productId": "guid",
      "name": "string",
      "sku": "string",
      "price": "decimal",
      "isActive": true,
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Get Product Variant By ID
**GET** `/api/admin/products/{productId:guid}/variants/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| productId | guid | Product ID |
| id | guid | Variant ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "productId": "guid",
  "name": "string",
  "sku": "string",
  "price": "decimal",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Create Product Variant
**POST** `/api/admin/products/{productId:guid}/variants`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| productId | guid | Product ID |

**Request Body:**
```json
{
  "productId": "guid (required)",
  "name": "string (required)",
  "sku": "string (required)",
  "price": "decimal (required)",
  "isActive": true
}
```

**Response:** `201 Created`
```json
{
  "id": "guid",
  "productId": "guid",
  "name": "string",
  "sku": "string",
  "price": "decimal",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Update Product Variant
**PUT** `/api/admin/products/{productId:guid}/variants/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| productId | guid | Product ID |
| id | guid | Variant ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "productId": "guid",
  "name": "string",
  "sku": "string",
  "price": "decimal",
  "isActive": true
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "productId": "guid",
  "name": "string",
  "sku": "string",
  "price": "decimal",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Delete Product Variant
**DELETE** `/api/admin/products/{productId:guid}/variants/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| productId | guid | Product ID |
| id | guid | Variant ID |

**Response:** `204 No Content`

---

## Admin: Product Images

**Requires AdminOnly policy.**

### Get Product Images
**GET** `/api/admin/products/{productId:guid}/images`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| productId | guid | Product ID |

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| productVariantId | guid | No | Filter by variant |
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "productId": "guid",
      "productVariantId": "guid?",
      "url": "string",
      "altText": "string",
      "displayOrder": 1,
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

## Admin: Product Attributes

**Requires AdminOnly policy.**

### Get All Product Attributes (Admin)
**GET** `/api/admin/attributes`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| search | string | No | Search term |
| isVariant | bool | No | Filter by variant attribute |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "name": "string",
      "code": "string",
      "description": "string",
      "isVariant": true,
      "isRequired": false,
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Get Product Attribute By ID (Admin)
**GET** `/api/admin/attributes/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Attribute ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "code": "string",
  "description": "string",
  "isVariant": true,
  "isRequired": false,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Create Product Attribute
**POST** `/api/admin/attributes`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "name": "string (required)",
  "code": "string (required)",
  "description": "string",
  "isVariant": false,
  "isRequired": false
}
```

**Response:** `201 Created`
```json
{
  "id": "guid",
  "name": "string",
  "code": "string",
  "description": "string",
  "isVariant": false,
  "isRequired": false,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Update Product Attribute
**PUT** `/api/admin/attributes/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Attribute ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "name": "string",
  "code": "string",
  "description": "string",
  "isVariant": false,
  "isRequired": false
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "code": "string",
  "description": "string",
  "isVariant": false,
  "isRequired": false,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Delete Product Attribute
**DELETE** `/api/admin/attributes/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Attribute ID |

**Response:** `204 No Content`

---

## Admin: Promotions

**Requires AdminOnly policy.**

### Get All Promotions (Admin)
**GET** `/api/admin/promotions`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| search | string | No | Search term |
| isActive | bool | No | Filter by active status |
| type | string | No | Filter by type |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "name": "string",
      "description": "string",
      "type": "string",
      "priority": 1,
      "rules": "string (JSON)",
      "isActive": true,
      "startsAt": "2026-08-18T00:00:00Z",
      "expiresAt": "2026-08-18T00:00:00Z",
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Get Promotion By ID (Admin)
**GET** `/api/admin/promotions/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Promotion ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "type": "string",
  "priority": 1,
  "rules": "string (JSON)",
  "isActive": true,
  "startsAt": "2026-08-18T00:00:00Z",
  "expiresAt": "2026-08-18T00:00:00Z",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Create Promotion
**POST** `/api/admin/promotions`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "name": "string (required)",
  "description": "string",
  "type": "string (required)",
  "priority": 1,
  "rules": "string (JSON)",
  "isActive": true,
  "startsAt": "2026-08-18T00:00:00Z",
  "expiresAt": "2026-08-18T00:00:00Z"
}
```

**Response:** `201 Created`
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "type": "string",
  "priority": 1,
  "rules": "string (JSON)",
  "isActive": true,
  "startsAt": "2026-08-18T00:00:00Z",
  "expiresAt": "2026-08-18T00:00:00Z",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Update Promotion
**PUT** `/api/admin/promotions/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Promotion ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "name": "string",
  "description": "string",
  "type": "string",
  "priority": 1,
  "rules": "string (JSON)",
  "isActive": true,
  "startsAt": "2026-08-18T00:00:00Z",
  "expiresAt": "2026-08-18T00:00:00Z"
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "type": "string",
  "priority": 1,
  "rules": "string (JSON)",
  "isActive": true,
  "startsAt": "2026-08-18T00:00:00Z",
  "expiresAt": "2026-08-18T00:00:00Z",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Delete Promotion
**DELETE** `/api/admin/promotions/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Promotion ID |

**Response:** `204 No Content`

---

## Admin: Payments

**Requires AdminOnly policy.**

### Get All Payments (Admin)
**GET** `/api/admin/payments`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| orderId | guid | No | Filter by order |
| status | string | No | Filter by status |
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "orderId": "guid",
      "amount": "decimal",
      "currency": "string",
      "status": "string",
      "paymentMethod": "string",
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Get Payment By ID (Admin)
**GET** `/api/admin/payments/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Payment ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "orderId": "guid",
  "amount": "decimal",
  "currency": "string",
  "status": "string",
  "paymentMethod": "string",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Capture Payment
**POST** `/api/admin/payments/{id:guid}/capture`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Payment ID |

**Request Body:**
```json
{
  "amount": "decimal? (optional)"
}
```

**Response:** `200 OK`
```json
{
  "paymentId": "guid",
  "status": "Captured",
  "amount": "decimal",
  "message": "string"
}
```

---

### Void Payment
**POST** `/api/admin/payments/{id:guid}/void`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Payment ID |

**Response:** `200 OK`
```json
{
  "paymentId": "guid",
  "status": "Voided",
  "amount": "decimal",
  "message": "string"
}
```

---

### Refund Payment
**POST** `/api/admin/payments/{id:guid}/refund`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Payment ID |

**Request Body:**
```json
{
  "amount": "decimal (required)",
  "reason": "string",
  "idempotencyKey": "string (required)"
}
```

**Response:** `200 OK`
```json
{
  "refundId": "guid",
  "paymentId": "guid",
  "status": "Succeeded",
  "amount": "decimal",
  "message": "string"
}
```

---

### Get All Refunds (Admin)
**GET** `/api/admin/payments/refunds`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| paymentId | guid | No | Filter by payment |
| status | string | No | Filter by status |
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "paymentId": "guid",
      "amount": "decimal",
      "reason": "string",
      "status": "string",
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Get Refund By ID (Admin)
**GET** `/api/admin/payments/refunds/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Refund ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "paymentId": "guid",
  "amount": "decimal",
  "reason": "string",
  "status": "string",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

## Admin: Shipping

**Requires AdminOnly policy.**

### Get Shipping Zones
**GET** `/api/admin/shipping/zones`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| search | string | No | Search term |
| isActive | bool | No | Filter by active status |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "name": "string",
      "description": "string",
      "isActive": true,
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Create Shipping Zone
**POST** `/api/admin/shipping/zones`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "name": "string (required)",
  "description": "string",
  "isActive": true
}
```

**Response:** `201 Created`
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Get Shipping Zone By ID
**GET** `/api/admin/shipping/zones/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Shipping zone ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Update Shipping Zone
**PUT** `/api/admin/shipping/zones/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Shipping zone ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "name": "string",
  "description": "string",
  "isActive": true
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Delete Shipping Zone
**DELETE** `/api/admin/shipping/zones/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Shipping zone ID |

**Response:** `204 No Content`

---

### Get Shipping Methods
**GET** `/api/admin/shipping/methods`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| zoneId | guid | No | Filter by zone |
| isActive | bool | No | Filter by active status |
| search | string | No | Search term |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "shippingZoneId": "guid",
      "name": "string",
      "description": "string",
      "estimatedDaysMin": 1,
      "estimatedDaysMax": 5,
      "isActive": true,
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Create Shipping Method
**POST** `/api/admin/shipping/methods`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "shippingZoneId": "guid (required)",
  "name": "string (required)",
  "description": "string",
  "estimatedDaysMin": 1,
  "estimatedDaysMax": 5,
  "isActive": true
}
```

**Response:** `201 Created`
```json
{
  "id": "guid",
  "shippingZoneId": "guid",
  "name": "string",
  "description": "string",
  "estimatedDaysMin": 1,
  "estimatedDaysMax": 5,
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Get Shipping Method By ID
**GET** `/api/admin/shipping/methods/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Shipping method ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "shippingZoneId": "guid",
  "name": "string",
  "description": "string",
  "estimatedDaysMin": 1,
  "estimatedDaysMax": 5,
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Update Shipping Method
**PUT** `/api/admin/shipping/methods/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Shipping method ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "shippingZoneId": "guid",
  "name": "string",
  "description": "string",
  "estimatedDaysMin": 1,
  "estimatedDaysMax": 5,
  "isActive": true
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "shippingZoneId": "guid",
  "name": "string",
  "description": "string",
  "estimatedDaysMin": 1,
  "estimatedDaysMax": 5,
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Delete Shipping Method
**DELETE** `/api/admin/shipping/methods/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Shipping method ID |

**Response:** `204 No Content`

---

### Get Shipping Rates
**GET** `/api/admin/shipping/rates`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| methodId | guid | No | Filter by method |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "shippingMethodId": "guid",
      "minWeight": "decimal",
      "maxWeight": "decimal",
      "price": "decimal",
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Create Shipping Rate
**POST** `/api/admin/shipping/rates`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "shippingMethodId": "guid (required)",
  "minWeight": "decimal (required)",
  "maxWeight": "decimal (required)",
  "price": "decimal (required)"
}
```

**Response:** `201 Created`
```json
{
  "id": "guid",
  "shippingMethodId": "guid",
  "minWeight": "decimal",
  "maxWeight": "decimal",
  "price": "decimal",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Get Shipping Rate By ID
**GET** `/api/admin/shipping/rates/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Shipping rate ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "shippingMethodId": "guid",
  "minWeight": "decimal",
  "maxWeight": "decimal",
  "price": "decimal",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Update Shipping Rate
**PUT** `/api/admin/shipping/rates/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Shipping rate ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "shippingMethodId": "guid",
  "minWeight": "decimal",
  "maxWeight": "decimal",
  "price": "decimal"
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "shippingMethodId": "guid",
  "minWeight": "decimal",
  "maxWeight": "decimal",
  "price": "decimal",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Delete Shipping Rate
**DELETE** `/api/admin/shipping/rates/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Shipping rate ID |

**Response:** `204 No Content`

---

## Admin: Tax

**Requires AdminOnly policy.**

### Get Tax Categories
**GET** `/api/admin/tax/categories`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| search | string | No | Search term |
| isActive | bool | No | Filter by active status |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "name": "string",
      "description": "string",
      "isActive": true,
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Create Tax Category
**POST** `/api/admin/tax/categories`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "name": "string (required)",
  "description": "string",
  "isActive": true
}
```

**Response:** `201 Created`
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Get Tax Category By ID
**GET** `/api/admin/tax/categories/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Tax category ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Update Tax Category
**PUT** `/api/admin/tax/categories/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Tax category ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "name": "string",
  "description": "string",
  "isActive": true
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Delete Tax Category
**DELETE** `/api/admin/tax/categories/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Tax category ID |

**Response:** `204 No Content`

---

### Get Tax Rates
**GET** `/api/admin/tax/rates`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| categoryId | guid | No | Filter by tax category |
| countryCode | string | No | Filter by country code |
| isActive | bool | No | Filter by active status |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "taxCategoryId": "guid",
      "countryCode": "US",
      "state": "string",
      "postalCodePattern": "string",
      "rate": "decimal",
      "isActive": true,
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Create Tax Rate
**POST** `/api/admin/tax/rates`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "taxCategoryId": "guid (required)",
  "countryCode": "string (required)",
  "state": "string",
  "postalCodePattern": "string",
  "rate": "decimal (required)",
  "isActive": true
}
```

**Response:** `201 Created`
```json
{
  "id": "guid",
  "taxCategoryId": "guid",
  "countryCode": "US",
  "state": "string",
  "postalCodePattern": "string",
  "rate": "decimal",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Get Tax Rate By ID
**GET** `/api/admin/tax/rates/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Tax rate ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "taxCategoryId": "guid",
  "countryCode": "US",
  "state": "string",
  "postalCodePattern": "string",
  "rate": "decimal",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Update Tax Rate
**PUT** `/api/admin/tax/rates/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Tax rate ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "taxCategoryId": "guid",
  "countryCode": "string",
  "state": "string",
  "postalCodePattern": "string",
  "rate": "decimal",
  "isActive": true
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "taxCategoryId": "guid",
  "countryCode": "US",
  "state": "string",
  "postalCodePattern": "string",
  "rate": "decimal",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Delete Tax Rate
**DELETE** `/api/admin/tax/rates/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Tax rate ID |

**Response:** `204 No Content`

---

## Admin: Notifications

**Requires AdminOnly policy.**

### Get All Notifications (Admin)
**GET** `/api/admin/notifications`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| type | string | No | Filter by type |
| channel | string | No | Filter by channel |
| status | string | No | Filter by status |
| userId | guid | No | Filter by user |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "userId": "guid",
      "type": "string",
      "channel": "string",
      "subject": "string",
      "body": "string",
      "status": "string",
      "provider": "string",
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Get Notification By ID (Admin)
**GET** `/api/admin/notifications/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Notification ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "userId": "guid",
  "type": "string",
  "channel": "string",
  "subject": "string",
  "body": "string",
  "status": "string",
  "provider": "string",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Create Notification
**POST** `/api/admin/notifications`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "userId": "guid (required)",
  "type": "string (required)",
  "channel": "string (required)",
  "subject": "string (required)",
  "body": "string (required)",
  "provider": "string"
}
```

**Response:** `201 Created`
```json
{
  "id": "guid",
  "userId": "guid",
  "type": "string",
  "channel": "string",
  "subject": "string",
  "body": "string",
  "status": "string",
  "provider": "string",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Update Notification
**PUT** `/api/admin/notifications/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Notification ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "userId": "guid",
  "type": "string",
  "channel": "string",
  "subject": "string",
  "body": "string",
  "status": "string",
  "provider": "string"
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "userId": "guid",
  "type": "string",
  "channel": "string",
  "subject": "string",
  "body": "string",
  "status": "string",
  "provider": "string",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Delete Notification
**DELETE** `/api/admin/notifications/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Notification ID |

**Response:** `204 No Content`

---

### Get Notification Templates (Admin)
**GET** `/api/admin/notifications/templates`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| search | string | No | Search term |
| channel | string | No | Filter by channel |
| isActive | bool | No | Filter by active status |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "name": "string",
      "channel": "string",
      "subject": "string",
      "body": "string",
      "isActive": true,
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Create Notification Template
**POST** `/api/admin/notifications/templates`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "name": "string (required)",
  "channel": "string (required)",
  "subject": "string (required)",
  "body": "string (required)",
  "isActive": true
}
```

**Response:** `201 Created`
```json
{
  "id": "guid",
  "name": "string",
  "channel": "string",
  "subject": "string",
  "body": "string",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Get Notification Template By ID (Admin)
**GET** `/api/admin/notifications/templates/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Template ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "channel": "string",
  "subject": "string",
  "body": "string",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Update Notification Template
**PUT** `/api/admin/notifications/templates/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Template ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "name": "string",
  "channel": "string",
  "subject": "string",
  "body": "string",
  "isActive": true
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "channel": "string",
  "subject": "string",
  "body": "string",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Delete Notification Template
**DELETE** `/api/admin/notifications/templates/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Template ID |

**Response:** `204 No Content`

---

### Get Notification Preferences (Admin)
**GET** `/api/admin/notifications/preferences`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| userId | guid | No | Filter by user |
| notificationType | string | No | Filter by notification type |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "userId": "guid",
      "notificationType": "string",
      "channel": "string",
      "isEnabled": true,
      "updatedAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Update Notification Preference
**PUT** `/api/admin/notifications/preferences/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Preference ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "userId": "guid",
  "notificationType": "string",
  "channel": "string",
  "isEnabled": true
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "userId": "guid",
  "notificationType": "string",
  "channel": "string",
  "isEnabled": true,
  "updatedAt": "2026-08-18T00:00:00Z"
}
```

---

### Get Notification Channels (Admin)
**GET** `/api/admin/notifications/channels`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| isActive | bool | No | Filter by active status |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "name": "string",
      "channelType": "string",
      "configuration": "string (JSON)",
      "isActive": true,
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Create Notification Channel
**POST** `/api/admin/notifications/channels`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "name": "string (required)",
  "channelType": "string (required)",
  "configuration": "string (JSON)",
  "isActive": true
}
```

**Response:** `201 Created`
```json
{
  "id": "guid",
  "name": "string",
  "channelType": "string",
  "configuration": "string (JSON)",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Get Notification Channel By ID (Admin)
**GET** `/api/admin/notifications/channels/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Channel ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "channelType": "string",
  "configuration": "string (JSON)",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Update Notification Channel
**PUT** `/api/admin/notifications/channels/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Channel ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "name": "string",
  "channelType": "string",
  "configuration": "string (JSON)",
  "isActive": true
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "channelType": "string",
  "configuration": "string (JSON)",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Delete Notification Channel
**DELETE** `/api/admin/notifications/channels/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Channel ID |

**Response:** `204 No Content`

---

## Admin: Reports

**Requires AdminOnly policy.**

### Get Sales Report
**GET** `/api/admin/reports/sales`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| startDate | datetime | No | Start date |
| endDate | datetime | No | End date |
| groupBy | string | No | Group by: day, week, month (default: day) |

**Response:** `200 OK`
```json
{
  "startDate": "2026-08-18T00:00:00Z",
  "endDate": "2026-08-18T00:00:00Z",
  "groupBy": "day",
  "data": [
    {
      "date": "2026-08-18T00:00:00Z",
      "totalSales": "decimal",
      "orderCount": 10
    }
  ]
}
```

---

### Get Revenue Report
**GET** `/api/admin/reports/revenue`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| startDate | datetime | No | Start date |
| endDate | datetime | No | End date |
| groupBy | string | No | Group by: day, week, month (default: day) |

**Response:** `200 OK`
```json
{
  "startDate": "2026-08-18T00:00:00Z",
  "endDate": "2026-08-18T00:00:00Z",
  "groupBy": "day",
  "data": [
    {
      "date": "2026-08-18T00:00:00Z",
      "totalRevenue": "decimal",
      "netRevenue": "decimal"
    }
  ]
}
```

---

### Get Inventory Report
**GET** `/api/admin/reports/inventory`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| asOfDate | datetime | No | As of date |
| warehouseIds | array[guid] | No | Filter by warehouses |
| categoryIds | array[guid] | No | Filter by categories |

**Response:** `200 OK`
```json
{
  "asOfDate": "2026-08-18T00:00:00Z",
  "totalItems": 100,
  "totalValue": "decimal",
  "lowStockItems": 5,
  "items": [
    {
      "productId": "guid",
      "productName": "string",
      "sku": "string",
      "quantity": 100,
      "reorderPoint": 10,
      "value": "decimal"
    }
  ]
}
```

---

### Get Customer Report
**GET** `/api/admin/reports/customers`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| startDate | datetime | No | Start date |
| endDate | datetime | No | End date |

**Response:** `200 OK`
```json
{
  "startDate": "2026-08-18T00:00:00Z",
  "endDate": "2026-08-18T00:00:00Z",
  "totalCustomers": 100,
  "newCustomers": 20,
  "returningCustomers": 80,
  "topCustomers": [
    {
      "userId": "guid",
      "email": "string",
      "orderCount": 10,
      "totalSpent": "decimal"
    }
  ]
}
```

---

### Export Report
**POST** `/api/admin/reports/export`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "reportType": "sales",
  "startDate": "2026-08-18T00:00:00Z",
  "endDate": "2026-08-18T00:00:00Z",
  "groupBy": "day",
  "categoryIds": ["guid"],
  "warehouseIds": ["guid"],
  "format": "csv"
}
```

**Response:** `200 OK` (File download)
- Content-Type: `text/csv` or `application/json`
- File name: `sales-report-2026-08-18.csv`

---

## Admin: Inventory

**Requires AdminOnly policy.**

### Get All Inventory Items (Admin)
**GET** `/api/admin/inventory`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| search | string | No | Search term |
| productId | guid | No | Filter by product |
| warehouseId | guid | No | Filter by warehouse |
| lowStockOnly | bool | No | Filter by low stock only |
| includeBackorder | bool | No | Include backorder items (default: false) |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "productId": "guid",
      "warehouseId": "guid",
      "quantity": 100,
      "reorderPoint": 10,
      "maxStock": 1000,
      "isBackordered": false,
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Get Inventory Item By ID (Admin)
**GET** `/api/admin/inventory/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Inventory item ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "productId": "guid",
  "warehouseId": "guid",
  "quantity": 100,
  "reorderPoint": 10,
  "maxStock": 1000,
  "isBackordered": false,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Adjust Inventory
**POST** `/api/admin/inventory/{id:guid}/adjust`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Inventory item ID |

**Request Body:**
```json
{
  "inventoryItemId": "guid (required)",
  "quantityChange": 10,
  "reason": "string"
}
```

**Response:** `204 No Content`

---

### Transfer Inventory
**POST** `/api/admin/inventory/{id:guid}/transfer`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Inventory item ID |

**Request Body:**
```json
{
  "inventoryItemId": "guid (required)",
  "toWarehouseId": "guid (required)",
  "quantity": 10,
  "reason": "string"
}
```

**Response:** `204 No Content`

---

### Set Reorder Point
**PUT** `/api/admin/inventory/{id:guid}/reorder-point`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Inventory item ID |

**Request Body:**
```json
{
  "inventoryItemId": "guid (required)",
  "reorderPoint": 10,
  "maxStock": 1000
}
```

**Response:** `204 No Content`

---

## Admin: Audit Logs

**Requires AdminOnly policy.**

### Get All Audit Logs (Admin)
**GET** `/api/admin/audit-logs`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| entityName | string | No | Filter by entity name |
| action | string | No | Filter by action |
| userId | guid | No | Filter by user |
| fromDate | datetime | No | Filter from date |
| toDate | datetime | No | Filter to date |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "entityName": "string",
      "entityId": "guid",
      "action": "string",
      "userId": "guid",
      "changes": "string (JSON)",
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Get Audit Log By ID (Admin)
**GET** `/api/admin/audit-logs/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Audit log ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "entityName": "string",
  "entityId": "guid",
  "action": "string",
  "userId": "guid",
  "changes": "string (JSON)",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

## Admin: Currencies

**Requires AdminOnly policy.**

### Get All Currencies (Admin)
**GET** `/api/admin/currencies`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "code": "USD",
      "name": "US Dollar",
      "symbol": "$",
      "isBaseCurrency": true,
      "isActive": true,
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Get Currency By ID (Admin)
**GET** `/api/admin/currencies/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Currency ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "code": "USD",
  "name": "US Dollar",
  "symbol": "$",
  "isBaseCurrency": true,
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Create Currency
**POST** `/api/admin/currencies`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "code": "string (required)",
  "name": "string (required)",
  "symbol": "string",
  "isBaseCurrency": false,
  "isActive": true
}
```

**Response:** `201 Created`
```json
{
  "id": "guid",
  "code": "USD",
  "name": "US Dollar",
  "symbol": "$",
  "isBaseCurrency": false,
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Update Currency
**PUT** `/api/admin/currencies/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Currency ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "code": "string",
  "name": "string",
  "symbol": "string",
  "isBaseCurrency": false,
  "isActive": true
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "code": "USD",
  "name": "US Dollar",
  "symbol": "$",
  "isBaseCurrency": false,
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Delete Currency
**DELETE** `/api/admin/currencies/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Currency ID |

**Response:** `204 No Content`

---

## Admin: Exchange Rates

**Requires AdminOnly policy.**

### Get All Exchange Rates (Admin)
**GET** `/api/admin/exchange-rates`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| fromCurrencyId | guid | No | Filter by from currency |
| toCurrencyId | guid | No | Filter by to currency |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "fromCurrencyId": "guid",
      "toCurrencyId": "guid",
      "rate": "decimal",
      "effectiveDate": "2026-08-18T00:00:00Z",
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Get Exchange Rate By ID (Admin)
**GET** `/api/admin/exchange-rates/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Exchange rate ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "fromCurrencyId": "guid",
  "toCurrencyId": "guid",
  "rate": "decimal",
  "effectiveDate": "2026-08-18T00:00:00Z",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Create Exchange Rate
**POST** `/api/admin/exchange-rates`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "fromCurrencyId": "guid (required)",
  "toCurrencyId": "guid (required)",
  "rate": "decimal (required)",
  "effectiveDate": "2026-08-18T00:00:00Z"
}
```

**Response:** `201 Created`
```json
{
  "id": "guid",
  "fromCurrencyId": "guid",
  "toCurrencyId": "guid",
  "rate": "decimal",
  "effectiveDate": "2026-08-18T00:00:00Z",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Update Exchange Rate
**PUT** `/api/admin/exchange-rates/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Exchange rate ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "fromCurrencyId": "guid",
  "toCurrencyId": "guid",
  "rate": "decimal",
  "effectiveDate": "2026-08-18T00:00:00Z"
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "fromCurrencyId": "guid",
  "toCurrencyId": "guid",
  "rate": "decimal",
  "effectiveDate": "2026-08-18T00:00:00Z",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Delete Exchange Rate
**DELETE** `/api/admin/exchange-rates/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Exchange rate ID |

**Response:** `204 No Content`

---

## Admin: Vendors

**Requires AdminOnly policy.**

### Get All Vendors (Admin)
**GET** `/api/admin/vendors`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| search | string | No | Search term |
| isActive | bool | No | Filter by active status |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "name": "string",
      "email": "string",
      "phoneNumber": "string",
      "isActive": true,
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Get Vendor By ID (Admin)
**GET** `/api/admin/vendors/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Vendor ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "email": "string",
  "phoneNumber": "string",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Create Vendor
**POST** `/api/admin/vendors`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "name": "string (required)",
  "email": "string",
  "phoneNumber": "string",
  "isActive": true
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "email": "string",
  "phoneNumber": "string",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Update Vendor
**PUT** `/api/admin/vendors/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Vendor ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "name": "string",
  "email": "string",
  "phoneNumber": "string",
  "isActive": true
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "email": "string",
  "phoneNumber": "string",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Delete Vendor
**DELETE** `/api/admin/vendors/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Vendor ID |

**Response:** `204 No Content`

---

### Get Vendor Products
**GET** `/api/admin/vendors/{id:guid}/products`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Vendor ID |

**Response:** `200 OK`
```json
[
  {
    "id": "guid",
    "vendorId": "guid",
    "productId": "guid",
    "productName": "string",
    "productSku": "string",
    "commissionRate": "decimal",
    "createdAt": "2026-08-18T00:00:00Z"
  }
]
```

---

### Add Product to Vendor
**POST** `/api/admin/vendors/{id:guid}/products`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Vendor ID |

**Request Body:**
```json
{
  "vendorId": "guid (required)",
  "productId": "guid (required)",
  "commissionRate": "decimal"
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "vendorId": "guid",
  "productId": "guid",
  "productName": "string",
  "productSku": "string",
  "commissionRate": "decimal",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Remove Product from Vendor
**DELETE** `/api/admin/vendors/products/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Vendor product ID |

**Response:** `204 No Content`

---

## Admin: Warehouses

**Requires AdminOnly policy.**

### Get All Warehouses (Admin)
**GET** `/api/admin/warehouses`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| search | string | No | Search term |
| isActive | bool | No | Filter by active status |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "name": "string",
      "addressLine1": "string",
      "city": "string",
      "state": "string",
      "postalCode": "string",
      "country": "string",
      "isActive": true,
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Get Warehouse By ID (Admin)
**GET** `/api/admin/warehouses/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Warehouse ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "addressLine1": "string",
  "city": "string",
  "state": "string",
  "postalCode": "string",
  "country": "string",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Create Warehouse
**POST** `/api/admin/warehouses`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "name": "string (required)",
  "addressLine1": "string (required)",
  "addressLine2": "string",
  "city": "string (required)",
  "state": "string (required)",
  "postalCode": "string (required)",
  "country": "string (required)",
  "isActive": true
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "addressLine1": "string",
  "city": "string",
  "state": "string",
  "postalCode": "string",
  "country": "string",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Update Warehouse
**PUT** `/api/admin/warehouses/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Warehouse ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "name": "string",
  "addressLine1": "string",
  "addressLine2": "string",
  "city": "string",
  "state": "string",
  "postalCode": "string",
  "country": "string",
  "isActive": true
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "addressLine1": "string",
  "city": "string",
  "state": "string",
  "postalCode": "string",
  "country": "string",
  "isActive": true,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Delete Warehouse
**DELETE** `/api/admin/warehouses/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Warehouse ID |

**Response:** `204 No Content`

---

## Admin: Support Tickets

**Requires AdminOnly policy.**

### Get All Support Tickets (Admin)
**GET** `/api/admin/support-tickets`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| status | string | No | Filter by status |
| priority | string | No | Filter by priority |
| assignedToUserId | guid | No | Filter by assignee |
| search | string | No | Search term |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "userId": "guid",
      "subject": "string",
      "status": "Open",
      "priority": "Medium",
      "assignedToUserId": "guid",
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Get Support Ticket By ID (Admin)
**GET** `/api/admin/support-tickets/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Support ticket ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "userId": "guid",
  "subject": "string",
  "status": "Open",
  "priority": "Medium",
  "assignedToUserId": "guid",
  "messages": [...],
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Update Support Ticket
**PUT** `/api/admin/support-tickets/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Support ticket ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "status": "string",
  "priority": "string",
  "assignedToUserId": "guid"
}
```

**Response:** `204 No Content`

---

## Admin: Reviews

**Requires AdminOnly policy.**

### Get All Reviews (Admin)
**GET** `/api/admin/reviews`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| productId | guid | No | Filter by product |
| isApproved | bool | No | Filter by approval status |
| minRating | int | No | Filter by minimum rating |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "productId": "guid",
      "userId": "guid",
      "rating": 5,
      "title": "string",
      "comment": "string",
      "isApproved": true,
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Update Review Status
**PUT** `/api/admin/reviews/{id:guid}/status`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Review ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "isApproved": true
}
```

**Response:** `204 No Content`

---

### Delete Review
**DELETE** `/api/admin/reviews/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Review ID |

**Response:** `204 No Content`

---

## Admin: Shipments

**Requires AdminOnly policy.**

### Get All Shipments (Admin)
**GET** `/api/admin/shipments`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| orderId | guid | No | Filter by order |
| status | string | No | Filter by status |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "orderId": "guid",
      "status": "Shipped",
      "carrier": "string",
      "trackingNumber": "string",
      "shippedAt": "2026-08-18T00:00:00Z",
      "deliveredAt": "2026-08-18T00:00:00Z",
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Get Shipment By ID (Admin)
**GET** `/api/admin/shipments/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Shipment ID |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "orderId": "guid",
  "status": "Shipped",
  "carrier": "string",
  "trackingNumber": "string",
  "shippedAt": "2026-08-18T00:00:00Z",
  "deliveredAt": "2026-08-18T00:00:00Z",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Create Shipment
**POST** `/api/admin/shipments`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "orderId": "guid (required)",
  "carrier": "string (required)",
  "trackingNumber": "string (required)",
  "shippedAt": "2026-08-18T00:00:00Z"
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "orderId": "guid",
  "status": "Shipped",
  "carrier": "string",
  "trackingNumber": "string",
  "shippedAt": "2026-08-18T00:00:00Z",
  "deliveredAt": "2026-08-18T00:00:00Z",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Update Shipment Status
**PUT** `/api/admin/shipments/{id:guid}/status`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Shipment ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "status": "string (required)"
}
```

**Response:** `204 No Content`

---

### Update Shipment Tracking
**PUT** `/api/admin/shipments/{id:guid}/tracking`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Shipment ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "carrier": "string (required)",
  "trackingNumber": "string (required)"
}
```

**Response:** `204 No Content`

---

## Admin: Tags

**Requires AdminOnly policy.**

### Get All Tags (Admin)
**GET** `/api/admin/tags`

**Headers:** `Authorization: Bearer <admin_token>`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |
| search | string | No | Search term |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "name": "string",
      "slug": "string",
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Create Tag
**POST** `/api/admin/tags`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "name": "string (required)",
  "slug": "string (required)"
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "slug": "string",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Update Tag
**PUT** `/api/admin/tags/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Tag ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "name": "string",
  "slug": "string"
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "slug": "string",
  "createdAt": "2026-08-18T00:00:00Z"
}
```

---

### Delete Tag
**DELETE** `/api/admin/tags/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Tag ID |

**Response:** `204 No Content`

---

## Admin: Categories

**Requires AdminOnly policy.**

### Get All Categories (Admin)
**GET** `/api/admin/categories`

**Headers:** `Authorization: Bearer <admin_token>`

**Response:** `200 OK`
```json
[
  {
    "id": "guid",
    "parentCategoryId": "guid?",
    "name": "string",
    "slug": "string",
    "description": "string",
    "imageUrl": "string",
    "displayOrder": 0,
    "isActive": true,
    "isFeatured": false,
    "children": []
  }
]
```

---

### Get Category By Slug
**GET** `/api/admin/categories/slug/{slug}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| slug | string | Category slug |

**Response:** `200 OK`
```json
{
  "id": "guid",
  "parentCategoryId": "guid?",
  "name": "string",
  "slug": "string",
  "description": "string",
  "imageUrl": "string",
  "displayOrder": 0,
  "isActive": true,
  "isFeatured": false,
  "children": []
}
```

---

### Create Category
**POST** `/api/admin/categories`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "parentCategoryId": "guid? (optional)",
  "name": "string (required)",
  "slug": "string (optional, auto-generated from name)",
  "description": "string",
  "imageUrl": "string",
  "displayOrder": 0,
  "isActive": true,
  "isFeatured": false,
  "metaTitle": "string",
  "metaDescription": "string"
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "parentCategoryId": "guid?",
  "name": "string",
  "slug": "string",
  "description": "string",
  "imageUrl": "string",
  "displayOrder": 0,
  "isActive": true,
  "isFeatured": false,
  "children": []
}
```

---

### Update Category
**PUT** `/api/admin/categories/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Category ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "parentCategoryId": "guid? (optional)",
  "name": "string",
  "slug": "string",
  "description": "string",
  "imageUrl": "string",
  "displayOrder": 0,
  "isActive": true,
  "isFeatured": false,
  "metaTitle": "string",
  "metaDescription": "string"
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "parentCategoryId": "guid?",
  "name": "string",
  "slug": "string",
  "description": "string",
  "imageUrl": "string",
  "displayOrder": 0,
  "isActive": true,
  "isFeatured": false,
  "children": []
}
```

---

### Delete Category
**DELETE** `/api/admin/categories/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Category ID |

**Response:** `204 No Content`

**Errors:**
- `400` Category has children - delete children first
- `400` Category has products - reassign products first
- `404` Category not found

---

## Admin: Brands

**Requires AdminOnly policy.**

### Get All Brands (Admin)
**GET** `/api/admin/brands`

**Headers:** `Authorization: Bearer <admin_token>`

**Response:** `200 OK`
```json
[
  {
    "id": "guid",
    "name": "string",
    "slug": "string",
    "description": "string",
    "imageUrl": "string",
    "isActive": true
  }
]
```

---

### Create Brand
**POST** `/api/admin/brands`

**Headers:** `Authorization: Bearer <admin_token>`

**Request Body:**
```json
{
  "name": "string (required)",
  "slug": "string (optional, auto-generated from name)",
  "description": "string",
  "imageUrl": "string",
  "isActive": true
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "slug": "string",
  "description": "string",
  "imageUrl": "string",
  "isActive": true
}
```

---

### Update Brand
**PUT** `/api/admin/brands/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Brand ID |

**Request Body:**
```json
{
  "id": "guid (required)",
  "name": "string",
  "slug": "string",
  "description": "string",
  "imageUrl": "string",
  "isActive": true
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "slug": "string",
  "description": "string",
  "imageUrl": "string",
  "isActive": true
}
```

---

### Delete Brand
**DELETE** `/api/admin/brands/{id:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Brand ID |

**Response:** `204 No Content`

**Errors:**
- `400` Brand has products - reassign products first
- `404` Brand not found

---

## Admin: Product Images

**Requires AdminOnly policy.**

### Get All Product Images (Admin)
**GET** `/api/admin/products/{productId:guid}/images`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| productId | guid | Product ID |

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| productVariantId | guid? | No | Filter by variant |
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 20) |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "productId": "guid",
      "productVariantId": "guid?",
      "url": "string",
      "altText": "string",
      "isPrimary": true,
      "sortOrder": 0,
      "createdAt": "2026-08-18T00:00:00Z"
    }
  ],
  "totalCount": 10,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

---

### Create Product Image
**POST** `/api/admin/products/{productId:guid}/images`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| productId | guid | Product ID |

**Request Body:**
```json
{
  "productId": "guid (required, must match path)",
  "productVariantId": "guid? (optional)",
  "url": "string (required)",
  "altText": "string",
  "isPrimary": false,
  "sortOrder": 0
}
```

**Response:** `200 OK`
```json
{
  "id": "guid",
  "productId": "guid",
  "productVariantId": "guid?",
  "url": "string",
  "altText": "string",
  "isPrimary": true,
  "sortOrder": 0,
  "createdAt": "2026-08-18T00:00:00Z"
}
```

**Notes:**
- If `isPrimary: true`, other primary images for the same product/variant are automatically unset
- `productId` in body must match the path parameter
- `productVariantId` is optional (null = product-level image)

---

### Delete Product Image
**DELETE** `/api/admin/products/{productId:guid}/images/{imageId:guid}`

**Headers:** `Authorization: Bearer <admin_token>`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| productId | guid | Product ID |
| imageId | guid | Image ID |

**Response:** `204 No Content`

**Errors:**
- `404` Image not found

---

## Notes

- All authenticated endpoints return `401 Unauthorized` if the token is missing or invalid.
- All admin endpoints require the `AdminOnly` policy (user must have the `Admin` role).
- All `AdminOrCustomer` endpoints require the user to have either `Admin` or `Customer` role.
- Pagination is supported on all list endpoints via `page` and `pageSize` query parameters.
- Soft delete is supported on most entities (use `?hardDelete=true` for permanent deletion).
- All dates are returned in UTC (`2026-08-18T00:00:00Z` format).
- All GUIDs are returned as strings.
- All decimal values are returned as numbers.
- Error responses follow RFC 7807 ProblemDetails format.
