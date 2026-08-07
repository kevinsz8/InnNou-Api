SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PURCHASEORDERLINE - GET EFFECTIVE BY PURCHASE ORDER IDS (BATCH)
   Batched sibling of sp_PurchaseOrderLine_GetEffective, for read paths that need the
   effective lines of MANY PurchaseOrders at once (e.g.
   ConsolidatedPurchaseOrderService.GetPdfAsync, which previously looped one call per
   member PurchaseOrder) without an N+1 round trip. Same STRING_SPLIT, no-dynamic-SQL
   list-passing convention as sp_User_GetPaged/sp_ArticlePrice_GetCurrentBatch.

   Unlike the single-PO SP, there's no @OrderId to scope by here — the caller already
   knows exactly which PurchaseOrderIds it wants (a consolidation's own member list), so
   this filters directly on pol.PurchaseOrderId IN (...). Same "latest APPLIED
   rectification wins, fall back to the original snapshot" resolution logic, same
   LINE_ADDED-while-still-pending exclusion — see sp_PurchaseOrderLine_GetEffective's own
   header comment and .claude/PurchaseOrderRectificationModule.md for the full rules.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrderLine_GetEffectiveByPurchaseOrderIds
(
    @PurchaseOrderIds VARCHAR(MAX)
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        pol.PurchaseOrderLineId, pol.PurchaseOrderLineToken,
        pol.PurchaseOrderId, po.PurchaseOrderToken,
        pol.OrderLineId, ol.OrderLineToken,
        pol.ArticleId, a.ArticleToken, a.Name AS ArticleName, a.SupplierId, s.Name AS SupplierName, a.FamilyId,
        pol.PurchaseUnitId, pu.Code AS PurchaseUnitCode,
        pol.PurchaseQuantity,
        pol.ContentUnitId, cu.Code AS ContentUnitCode,
        pol.ContentQuantity,
        COALESCE(effValues.NewQuantity, pol.Quantity) AS Quantity,
        COALESCE(effValues.NewUnitPrice, pol.UnitPrice) AS UnitPrice,
        COALESCE(effValues.NewCurrencyCode, pol.CurrencyCode) AS CurrencyCode,
        pol.BaseUnitPrice, pol.DiscountTypeId, dt.Code AS DiscountTypeCode, pol.DiscountValue,
        pol.CategoryId, pol.CategoryCode, pol.SubCategoryId, pol.SubCategoryCode,
        pol.Notes,
        pol.CreatedUtc, pol.CreatedBy, pol.LastUpdatedUtc, pol.LastUpdatedBy,
        CASE WHEN latestAction.ActionCode = 'LINE_CANCELLED' THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsCancelled
    FROM dbo.PurchaseOrderLine pol
    JOIN dbo.PurchaseOrder po  ON po.PurchaseOrderId = pol.PurchaseOrderId
    LEFT JOIN dbo.OrderLine ol  ON ol.OrderLineId      = pol.OrderLineId
    JOIN dbo.Articles a         ON a.ArticleId         = pol.ArticleId
    JOIN dbo.Suppliers s        ON s.SupplierId        = a.SupplierId
    JOIN dbo.UnitsOfMeasure pu  ON pu.UnitOfMeasureId  = pol.PurchaseUnitId
    JOIN dbo.UnitsOfMeasure cu  ON cu.UnitOfMeasureId  = pol.ContentUnitId
    LEFT JOIN dbo.DiscountTypes dt ON dt.DiscountTypeId = pol.DiscountTypeId
    -- Display values always come from the latest APPLIED quantity/price change, even if a LATER
    -- cancellation exists — a cancelled line still shows what it was last actually worth, it
    -- doesn't revert to the very first original snapshot. IsCancelled is resolved independently
    -- from the true latest APPLIED row (which may be the cancellation itself).
    OUTER APPLY (
        SELECT TOP 1 lr.NewQuantity, lr.NewUnitPrice, lr.NewCurrencyCode
        FROM dbo.PurchaseOrderLineRectifications lr
        JOIN dbo.PurchaseOrderRectifications r ON r.PurchaseOrderRectificationId = lr.PurchaseOrderRectificationId
        JOIN dbo.PurchaseOrderRectificationStatuses rs ON rs.PurchaseOrderRectificationStatusId = r.PurchaseOrderRectificationStatusId
        WHERE lr.PurchaseOrderLineId = pol.PurchaseOrderLineId AND rs.Code = 'APPLIED' AND lr.NewQuantity IS NOT NULL
        ORDER BY lr.PurchaseOrderLineRectificationId DESC
    ) effValues
    OUTER APPLY (
        SELECT TOP 1 la.Code AS ActionCode
        FROM dbo.PurchaseOrderLineRectifications lr
        JOIN dbo.PurchaseOrderRectifications r ON r.PurchaseOrderRectificationId = lr.PurchaseOrderRectificationId
        JOIN dbo.PurchaseOrderRectificationStatuses rs ON rs.PurchaseOrderRectificationStatusId = r.PurchaseOrderRectificationStatusId
        JOIN dbo.PurchaseOrderRectificationLineActions la ON la.PurchaseOrderRectificationLineActionId = lr.PurchaseOrderRectificationLineActionId
        WHERE lr.PurchaseOrderLineId = pol.PurchaseOrderLineId AND rs.Code = 'APPLIED'
        ORDER BY lr.PurchaseOrderLineRectificationId DESC
    ) latestAction
    WHERE pol.PurchaseOrderId IN (SELECT CAST(value AS INT) FROM STRING_SPLIT(@PurchaseOrderIds, ','))
      AND (
          pol.OrderLineId IS NOT NULL
          OR EXISTS (
              SELECT 1
              FROM dbo.PurchaseOrderLineRectifications addedLr
              JOIN dbo.PurchaseOrderRectifications addedR ON addedR.PurchaseOrderRectificationId = addedLr.PurchaseOrderRectificationId
              JOIN dbo.PurchaseOrderRectificationStatuses addedRs ON addedRs.PurchaseOrderRectificationStatusId = addedR.PurchaseOrderRectificationStatusId
              JOIN dbo.PurchaseOrderRectificationLineActions addedLa ON addedLa.PurchaseOrderRectificationLineActionId = addedLr.PurchaseOrderRectificationLineActionId
              WHERE addedLr.PurchaseOrderLineId = pol.PurchaseOrderLineId AND addedLa.Code = 'LINE_ADDED' AND addedRs.Code = 'APPLIED'
          )
      )
    ORDER BY pol.PurchaseOrderId, pol.PurchaseOrderLineId;
END;
GO
