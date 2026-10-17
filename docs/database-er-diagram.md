# Koochino (MyShop) — Core Database ER Diagram

> Reconstructed from the entity models (`MyShop.Core/Models`) and the SQL embedded in the
> repositories (`MyShop.Infrastructure/Repositories`). The DDL itself (tables, constraints,
> and the `koch.sp*` stored procedures) lives in the database and is not committed to this repo.
> Last reviewed: 2026-10 (main).

GitHub renders the diagram below natively.

## Entity relationship diagram — commerce & identity core

```mermaid
erDiagram
    SITES {
        int Id PK
        nvarchar Code UK
        nvarchar Name
    }
    SITE_DOMAINS {
        int Id PK
        int SiteId FK
        nvarchar Host UK
    }
    USERS {
        int Id PK
        nvarchar Username
        nvarchar PasswordHash
        nvarchar PasswordSalt
        nvarchar Email
        nvarchar PhoneNumber
        nvarchar NationalCode
        bit IsActive
        bit IsPhoneVerified
        nvarchar GoogleSubject
        int UserStatID
        nvarchar RoleId "legacy string column - issue 16"
        int SiteId FK
        datetime CreatedAt
    }
    ROLES {
        int Id PK
        nvarchar Name UK
    }
    USER_ROLES {
        int UserId PK
        int RoleId PK
    }
    USER_ACTIVITY_LOG {
        int Id PK
        int UserId FK
        nvarchar Action
        datetime CreatedAt
    }
    USER_IDENTITY_DOCUMENTS {
        int Id PK
        int UserId FK
        byte Type
        byte Status
        datetime CreatedAt
    }
    STORES {
        int StoreID PK
        nvarchar StoreName
        int UserID FK
        int SiteId FK
        int CompanyId
    }
    ORDERS {
        int Id PK
        nvarchar OrderNumber UK
        int UserId FK
        int CustomerId "legacy alias of UserId"
        int StoreId FK
        byte OrderType "0 normal 1 coupon 2 chancebox"
        byte SaleChannel "0 online 1 in person"
        bigint TotalAmount
        bigint TotalPrice "legacy pre-migration column"
        bigint DiscountAmount
        bigint FinalAmount
        bigint WalletAmountApplied
        int CouponUsageId FK
        int ChanceBoxCodeUsageId FK
        datetime OrderDate
        nvarchar Status "string holding tinyint values - issue 14"
        int SiteId FK
        datetime CreatedAt
        datetime UpdatedAt
    }
    ORDER_ITEMS {
        int Id PK
        int OrderId FK
        int ProductId FK
        int VariationId FK
        nvarchar Sku
        int Quantity
        bigint UnitPrice
        bigint LineTotal
        datetime CreatedAt
    }
    ORDER_STATUS_HISTORY {
        int Id PK
        int OrderId FK
        byte FromStatus
        byte ToStatus
        nvarchar Note
        int ChangedByUserId FK
        datetime CreatedAt
    }
    PRODUCTS {
        int Id PK
        nvarchar Title
        int Quantity "stock for simple products"
        int StoreId FK
        int SiteId FK
        bit IsActive
    }
    COMMERCE_PRODUCT_VARIATIONS {
        int Id PK
        int ProductId FK
        nvarchar Sku
        int Stock "locked with UPDLOCK on reserve"
        bit IsActive
    }
    COMMERCE_CARTS {
        int Id PK
        int SiteId FK
        int UserId FK "null for guest carts"
        nvarchar GuestKey "guest identity"
        int StoreId FK
        datetime CreatedAt
        datetime UpdatedAt
    }
    COMMERCE_CART_ITEMS {
        int Id PK
        int CartId FK
        int ProductId FK
        int Quantity
        bigint UnitPrice
        datetime CreatedAt
    }
    COMMERCE_STOCK_RESERVATIONS {
        int Id PK
        int CartId FK
        int ProductId FK
        int VariationId FK
        int Quantity
        byte Status "0 active 1 released 2 committed"
        datetime ExpiresAt
        int OrderId FK
    }
    COMMERCE_PAYMENTS {
        int Id PK
        int OrderId FK
        int PaymentMethodId FK
        bigint Amount
        nvarchar Provider "Zarinpal, manual, ..."
        nvarchar Authority
        nvarchar RefId
        byte Status
        byte FinalizedState
        datetime CreatedAt
        datetime PaidAt
    }
    COMMERCE_PAYMENT_ATTEMPTS {
        bigint Id PK
        int PaymentId FK
        nvarchar AttemptType
        nvarchar Authority
        nvarchar Status
        datetime CreatedAt
    }
    COMMERCE_PAYMENT_METHODS {
        int Id PK
        int SiteId FK
        nvarchar Title
        nvarchar Code
        nvarchar GatewayProvider
        nvarchar ConfigJson
        bit IsActive
    }
    COMMERCE_SHIPPING_METHODS {
        int Id PK
        int SiteId FK
        nvarchar Title
        bigint Fee
        bigint FreeOverAmount
        bit IsActive
    }
    COMMERCE_REFUNDS {
        int Id PK
        int OrderId FK
        int PaymentId FK
        bigint Amount
        byte Status "0 requested 1 completed 2 rejected"
        int CreatedByUserId FK
        datetime CreatedAt
    }
    COPON {
        int CoponID PK
        nvarchar CoponTitle
        nvarchar CoponCode UK
        datetime CoponStartDate
        datetime CoponExpireDate
        int CoponStock
        int StoreID FK
        int UserID FK
        bit CoponStatStor
        bit CoponStatAdmin
        byte DiscountType "0 fixed 1 percent"
        bigint DiscountValue
    }
    COUPON_USAGE {
        int Id PK
        int CoponID FK
        int UserId FK
        int RedeemStoreId FK
        bigint ReservedAmount
        int OrderId FK
        datetime UsedAt
        bigint PurchaseAmount
        bigint DiscountAmount
    }
    CHANCE_BOX_CODE_USAGE {
        int Id PK
        int UserId FK
        int OrderId FK
        datetime UsedAt
    }
    USERS ||--o{ USER_ROLES : has
    ROLES ||--o{ USER_ROLES : grants
    SITES ||--o{ SITE_DOMAINS : maps hosts
    SITES ||--o{ USERS : brands
    SITES ||--o{ STORES : brands
    SITES ||--o{ ORDERS : scopes
    USERS |o--o{ STORES : owns
    USERS ||--o{ ORDERS : places
    USERS ||--o{ USER_ACTIVITY_LOG : generates
    USERS ||--o{ USER_IDENTITY_DOCUMENTS : submits
    STORES ||--o{ ORDERS : receives
    STORES ||--o{ COPON : issues
    USERS ||--o{ COUPON_USAGE : redeems
    COPON ||--o{ COUPON_USAGE : tracked by
    COUPON_USAGE |o--o| ORDERS : discounts
    CHANCE_BOX_CODE_USAGE |o--o| ORDERS : "redeemed by"
    ORDERS ||--o{ ORDER_ITEMS : contains
    ORDERS ||--o{ ORDER_STATUS_HISTORY : tracks
    ORDERS ||--o{ COMMERCE_PAYMENTS : "paid via"
    ORDERS |o--o{ COMMERCE_STOCK_RESERVATIONS : holds
    COMMERCE_PAYMENTS ||--o{ COMMERCE_PAYMENT_ATTEMPTS : logs
    COMMERCE_PAYMENTS ||--o{ COMMERCE_REFUNDS : "refunded by"
    COMMERCE_PAYMENT_METHODS |o--o{ COMMERCE_PAYMENTS : "configured as"
    PRODUCTS ||--o{ ORDER_ITEMS : "sold as"
    PRODUCTS ||--o{ COMMERCE_PRODUCT_VARIATIONS : has
    PRODUCTS ||--o{ COMMERCE_STOCK_RESERVATIONS : reserved
    COMMERCE_PRODUCT_VARIATIONS ||--o{ COMMERCE_STOCK_RESERVATIONS : reserved
    COMMERCE_CARTS ||--o{ COMMERCE_CART_ITEMS : holds
    USERS |o--o{ COMMERCE_CARTS : "user or guest"
    COMMERCE_CARTS ||--o{ COMMERCE_STOCK_RESERVATIONS : reserves
```

## Notable schema observations

| Observation | Impact | Issue |
|---|---|---|
| `Users.RoleId` is a legacy string column next to the real `UserRoles` table | Write-during-login role sync, duplicate-row race | [#16](https://github.com/PsymoNiko/koochino/issues/16) |
| `Orders.Status` is NVARCHAR holding tinyint values | `TRY_CAST` per row blocks index use | [#14](https://github.com/PsymoNiko/koochino/issues/14) |
| `Orders` has both `UserId` and legacy `CustomerId`, `TotalAmount` and legacy `TotalPrice` | `ISNULL()`/fallback logic in every query | [#14](https://github.com/PsymoNiko/koochino/issues/14) |
| `Users.PhoneNumber` matched with `REPLACE()/RIGHT()` in SQL | Full table scan per login | [#13](https://github.com/PsymoNiko/koochino/issues/13) |
| Every business table carries `SiteId` for multi-tenancy | Site filter currently applied post-pagination and fail-open | [#15](https://github.com/PsymoNiko/koochino/issues/15) |
| Coupon table named `Copon` with string money columns (`CoponMinBuy`) | Naming/type inconsistency | — |

## Out of scope

The Academy, CRM, Tickets, Warehouse, Blog, Points/Loyalty, Support chat, and
Module-settings domains exist in the repositories but are not expanded here.
The stored procedures (`spUsers_*`, `spOrders_*`, `spCopon_*`, ...) are not
committed to the repo and should be exported to version control (see the
accompanying performance doc).
