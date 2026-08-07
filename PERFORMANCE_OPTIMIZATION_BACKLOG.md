# InnNou — Backend Call & Round-Trip Optimization Backlog

**Status: all 5 items implemented 2026-08-07 (Phase 1: #1-#3, Phase 2: #4-#5).** Grew out of a question about whether InnNou's current call volume affects cloud hosting cost (see `CLOUD_HOSTING_ASSESSMENT.md`) — short answer there was "not directly, on the recommended flat-capacity plans," but reducing chattiness is still good practice: it delays the point where a bigger/pricier compute or DB tier is needed as usage grows, and it's a straight latency win regardless of billing model. Decided to act on all of it the same day since InnNou is still early-stage and can afford the engineering time now.

Findings from two parallel read-only audits (frontend HTTP-call-count-per-page-load, backend SP/round-trip-count-per-request), ranked by frequency of use × impact — how often the flow runs in a typical hotel back-office work day matters more than raw call count.

## 1. ✅ `OrderService.AddLineAsync` — 8–9 SP round trips for the single most-used action in the app — IMPLEMENTED

Every "add this article to the order" click — the highest-frequency action in all of InnNou — triggered this sequence server-side: order lookup, hierarchy check, `sp_Article_GetByToken`, `sp_ArticlePackagingLevel_GetByArticleId`, `sp_SupplierDeliveryZone_CheckCoverage`, `sp_ArticlePrice_GetCurrent`, `sp_ArticleDiscount_GetEffective`, `sp_ArticleClassification_GetEffectiveForArticle`, then the `sp_OrderLine_Upsert` write. Five of those (packaging, zone coverage, price, discount, classification) were independent lookups keyed off the same `ArticleId`/`OrganizationId`/`WarehouseId` already resolved a few lines earlier.

**Shipped 2026-08-07**: new `sp_Article_ResolveOrderLineDetails` (`database/stored-procedures/Article/`) returns those 5 lookups as multiple result sets in one round trip, read via Dapper's `QueryMultipleAsync` in `OrderService.AddLineAsync`. Article resolution itself (`sp_Article_GetByToken`) deliberately stayed a separate call — its ~46-column hierarchy/visibility projection has no stable contract worth mirroring via `INSERT...EXEC`, so the fix targets only the 5 calls that genuinely are independent, uniform lookups. `AddLineAsync`'s public signature and every validation branch/error code are unchanged — this was a pure data-access refactor, verified by the full integration suite (`ArticleDiscountTests.cs` covers every branch touched) passing unchanged. `CopyOrderAsync`/`ImportLinesAsync` inherit the fix for free since they call `AddLineAsync` internally. Net: 5 round trips → 1.

## 2. ✅ Catalog filter dropdowns fetch on page mount instead of on first open — IMPLEMENTED

The `useFamilySearch`/`useCategorySearch`/`useSupplierSearch`/`useSubFamilySearch`/`useSubCategorySearch` hooks built the same day for the searchable-selector conversion all auto-fetched page 1 as soon as they mounted — not when the user actually opened that dropdown. This hit the app's busiest screens: `OrderDetail.tsx`'s add-articles modal, `Articles.tsx`, `ArticleFavorites.tsx`, `OrderTemplateDetail.tsx`, both Inventory filter bars, `ArticleDiscountsPage.tsx`, `FamilyApprovalThresholds.tsx` — each fired 2–3 avoidable calls per page load, whether or not the user ever touched those filters.

**Shipped 2026-08-07**: each hook now exposes an `ensureLoaded()` that only fetches the first time it's called (guarded by a ref), instead of an eager mount-time `useEffect`. `SearchableSelect.tsx` gained an `onOpen` prop, called once when the dropdown opens, wired to each call site's `ensureLoaded`. Parent-token-scoped hooks (`useSubFamilySearch`/`useSubCategorySearch`) still clear stale results immediately when their parent token changes, just without eagerly re-fetching. `useSupplierSearch`'s existing "refresh when `warehouseToken` resolves asynchronously" behavior was preserved, gated on having been opened at least once. Verified via `tsc -b`/`npm run build`/lint, all clean, across all 9 files.

## 3. ✅ `ArticleClassification.tsx` — per-row selects fire up to ~40 calls just to render a page — IMPLEMENTED (as a side effect of #2)

The `ArticleClassificationRow` component reuses the same shared `useCategorySearch()`/`useSubCategorySearch()` hooks as everywhere else — fixing #2 at the hook level fixed this for free, no separate per-row code change needed. A page of ~20 rows now fires zero Category/SubCategory calls until a user actually opens one of those dropdowns.

## 4. ✅ No batch "add N lines to an order" endpoint — IMPLEMENTED

`AddOrderLineCommandRequest` only accepted one article at a time; `ImportLinesAsync` (Excel-based) was the only existing batch path. A buyer adding 5–8 catalog items to a cart used to fire 5–8 full `AddLineAsync` round trips, each independently redoing the order lookup + hierarchy check.

**Shipped 2026-08-07**: new `POST /orders/addLines` (`AddOrderLinesCommandRequest`, up to 100 lines) validates the Order once, then adds every line via a new shared `OrderService.AddLineToValidatedOrderAsync` helper — everything `AddLineAsync` used to do *after* its own order lookup/hierarchy/Draft checks, now callable directly with an already-resolved `Order`. `AddLineAsync` itself became a thin wrapper (resolve+validate, then call the helper) — its public signature/behavior is unchanged. `ImportLinesAsync`'s Excel-row loop was found to have the *identical* redundant-lookup problem (it called the public `AddLineAsync` per row) and got the same fix for free, switched to call the shared helper too. Best-effort semantics, same convention as `ImportLinesAsync`: one line's failure never aborts the rest, each failure reports its `Index`/`ArticleToken`/error back to the caller (`AddOrderLinesResultDto`). New regression tests: `tests/InnNou.IntegrationTests/Orders/AddOrderLinesBatchTests.cs` (all-succeed, partial-failure, order-not-found, empty-batch-rejected).

## 5. ✅ `SupplierInvoiceService`/`SupplierCreditNoteService`.HydrateAsync — 3 SP calls each per detail view — IMPLEMENTED

Same multi-result-set consolidation opportunity as #1 (Lines, TaxBreakdown, PurchaseOrders/CorrectedInvoices), called from `GetByTokenAsync` and right after every `CreateAsync`.

**Shipped 2026-08-07**: two new SPs (`sp_SupplierInvoice_HydrateDetails`, `sp_SupplierCreditNote_HydrateDetails`) each fan out to their 3 existing sub-procedures as multiple result sets, read via `QueryMultipleAsync` in one round trip. Simpler than #1's fix — no OUTPUT params or conditional branching, just 3 independent `SELECT`s keyed by the header's own Id. Both `HydrateAsync` private methods keep their exact same signature, so every call site benefits with zero other changes.

## Lower priority — flagged for completeness, not urgency

- **`Articles.tsx`** double-mounts `useFamilySearch()`/`useSupplierSearch()` independently in the filter bar and the create/edit form — 2x the calls of a shared instance, but only matters while the edit form is actually open.
- **`OrderService.GetByTokenAsync`** — 4 SP calls (header, hierarchy check, Lines, ApprovalSteps) per order detail view. Already reasonably lean; three of the four are genuinely separate collections with different fan-outs. Combining would need a multi-result-set SP for modest savings.

## Confirmed already fine — no action needed

- **Dashboard** is already a single HTTP round trip for all 6 `sp_Dashboard_Get*` calls (`DashboardEndpoints.cs`/`DashboardService.cs` fan out server-side) — this is the pattern everything above should move toward, not a problem itself.
- **No polling found anywhere in the frontend.** The only `setInterval` in the codebase is a local 1-second UI countdown for the session-expiry warning (no backend call per tick) — all real-time data correctly goes through SignalR push.
- **Bulk import services** (Articles, Users, Suppliers, etc.) process rows strictly sequentially by documented design — admin-only, infrequent, not latency-sensitive. Not worth relitigating.
- **`RequisitionService`/`InternalOrderService`/`PurchaseOrderService.RectifyAsync`'s per-line loops** were already addressed (Requisitions, batched same day) or deliberately left as-is (InternalOrderService/RectifyAsync — their per-line lookups need real hierarchy/visibility enforcement that's risky to batch; see the 2026-08-07 audit finding #9 writeup) earlier today. Excluded from this list on purpose.

---
*Compiled from two parallel read-only research passes (frontend call-count-per-page-load, backend SP/round-trip-count-per-request) run 2026-08-07, following up on a question about whether InnNou's current call volume affects cloud hosting cost. See `CLOUD_HOSTING_ASSESSMENT.md` for that context.*
