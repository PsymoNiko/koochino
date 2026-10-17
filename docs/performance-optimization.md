# Performance Optimization Roadmap

> Consolidated before/after comparison of the platform's hot paths.
> Source: issue [#20](https://github.com/PsymoNiko/koochino/issues/20), cross-referencing
> issues #5, #6, #10, #13–#17. Last reviewed: 2026-10 (main).

## Login & auth hot path

| Hotspot | Today | After fix | Expected gain | Issue |
|---|---|---|---|---|
| Phone lookup on login | `REPLACE/LTRIM/RIGHT()` on `PhoneNumber` per row → full scan of Users on every attempt | Write-time normalization into `NormalizedPhone` + index → equality index seek | O(N rows) → O(log N), stays flat as users grow | [#13](https://github.com/PsymoNiko/koochino/issues/13) |
| Login fallback chain | Inline query → legacy query → SP fallback: 3–4 sequential round trips on a miss | One query; legacy SPs retired to a one-time migration | ~3× fewer round trips per login | [#13](https://github.com/PsymoNiko/koochino/issues/13), [#16](https://github.com/PsymoNiko/koochino/issues/16) |
| Role sync during login | `INSERT UserRoles` on the read path (+ duplicate-row race) | Unique constraint + one-time migration; reads are pure reads | Removes write latency and lock contention | [#16](https://github.com/PsymoNiko/koochino/issues/16) |
| Email lookup | `LOWER/LTRIM/RTRIM` on column → full scan | Normalized/lowercase indexed column | Scan → seek | [#13](https://github.com/PsymoNiko/koochino/issues/13) |
| Anonymous `/find/{phone}` | Same full scan; per-URL rate key; unbounded static dictionary | Per-IP rate limit + normalized seek + bounded limiter | Enumeration cost up by orders of magnitude; memory flat | [#5](https://github.com/PsymoNiko/koochino/issues/5), [#6](https://github.com/PsymoNiko/koochino/issues/6) |

## Order & commerce paths

| Hotspot | Today | After fix | Expected gain | Issue |
|---|---|---|---|---|
| Order list / dashboard filters | `CAST(ISNULL(OrderDate, CreatedAt) AS DATE)` + `TRY_CAST(Status AS TINYINT)` per row → full scans | Computed date column + TINYINT status migration + covering indexes `(SiteId, StoreId, Status, EffectiveOrderDate DESC)` + dynamic WHERE | Scan → seek on the fastest-growing table | [#14](https://github.com/PsymoNiko/koochino/issues/14) |
| Tenant site filtering | Applied after `OFFSET/FETCH` + a second `SELECT Id, SiteId` per page; fails open on error | SiteId predicate inside the main query, returned in SELECT, fail closed | 2 round trips → 1; correct page sizes; no cross-site leakage | [#15](https://github.com/PsymoNiko/koochino/issues/15) |
| Pending orders queue | Hard cap 500 rows, silent truncation | Proper paging or documented cap + count | No invisible backlog | [#15](https://github.com/PsymoNiko/koochino/issues/15) |
| Coupon redemption | Validate and record-usage SPs as separate calls (TOCTOU race) | Atomic conditional decrement in one transaction (pattern already in `CommerceStockReservationRepository`) | No overselling under concurrency | [#17](https://github.com/PsymoNiko/koochino/issues/17) |

## Cross-cutting

| Area | Today | After fix | Expected gain | Issue |
|---|---|---|---|---|
| Rate limiting | Custom static `ConcurrentDictionary`, never cleaned, keyed by IP+URL | ASP.NET Core `RateLimiter` partitioned by IP | Fixed memory; limits that bind | [#6](https://github.com/PsymoNiko/koochino/issues/6) |
| OTP & counters | `IMemoryCache` (restart wipes, instances diverge) | DB table or distributed cache | Correct across restarts and scale-out | [#10](https://github.com/PsymoNiko/koochino/issues/10) |
| Order inserts | SP insert → separate `UPDATE SiteId` → separate coupon-usage calls, no transaction | Single transaction (or one atomic SP) | No half-committed orders; fewer round trips | [#17](https://github.com/PsymoNiko/koochino/issues/17) |

## How to verify the wins

1. Enable **SQL Server Query Store** on the production DB and snapshot the top queries
   **before** any change — the `REPLACE(...)` Users scan and the Orders list plan will
   dominate by logical reads.
2. Add **App Insights / OpenTelemetry** dependencies on the auth and order-list endpoints
   for per-request latency percentiles.
3. Apply one fix at a time, then re-snapshot. Priority order:
   [#13](https://github.com/PsymoNiko/koochino/issues/13) →
   [#14](https://github.com/PsymoNiko/koochino/issues/14) →
   [#15](https://github.com/PsymoNiko/koochino/issues/15) →
   [#6](https://github.com/PsymoNiko/koochino/issues/6) →
   [#10](https://github.com/PsymoNiko/koochino/issues/10).
4. Keep the `MyShop.Tests` suite green — it already covers the permission/scope behavior
   these changes must preserve.
