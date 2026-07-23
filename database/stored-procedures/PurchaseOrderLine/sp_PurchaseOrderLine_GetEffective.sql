SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PURCHASEORDERLINE - GET EFFECTIVE
   Resolves each line's CURRENT value — Quantity/UnitPrice/CurrencyCode
   as last corrected by an APPLIED PurchaseOrderRectification, falling
   back to the original PurchaseOrderLine snapshot when none has ever
   applied. Same "latest wins" resolution shape as
   sp_ArticlePrice_GetCurrent, scoped per-line instead of per-article.
   PurchaseOrderLine itself is NEVER mutated — see
   .claude/PurchaseOrderRectificationModule.md.

   @OrderId scopes to every PurchaseOrder split from the same originating
   Order (needed for PurchaseOrderService's cross-supplier Family-total
   recompute); @PurchaseOrderId optionally narrows to a single PurchaseOrder
   (the normal single-PO detail/PDF/totals read path). Both together:
   WHERE po.OrderId = @OrderId AND (@PurchaseOrderId IS NULL OR ...).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrderLine_GetEffective
(
    @OrderId          INT,
    @PurchaseOrderId  INT = NULL
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
        pol.CategoryId, pol.CategoryCode, pol.SubCategoryId, pol.SubCategoryCode,
        pol.Notes,
        pol.CreatedUtc, pol.CreatedBy, pol.LastUpdatedUtc, pol.LastUpdatedBy,
        CASE WHEN latestAction.ActionCode = 'LINE_CANCELLED' THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsCancelled
    FROM dbo.PurchaseOrderLine pol
    JOIN dbo.PurchaseOrder po  ON po.PurchaseOrderId = pol.PurchaseOrderId
    JOIN dbo.OrderLine ol       ON ol.OrderLineId      = pol.OrderLineId
    JOIN dbo.Articles a         ON a.ArticleId         = pol.ArticleId
    JOIN dbo.Suppliers s        ON s.SupplierId        = a.SupplierId
    JOIN dbo.UnitsOfMeasure pu  ON pu.UnitOfMeasureId  = pol.PurchaseUnitId
    JOIN dbo.UnitsOfMeasure cu  ON cu.UnitOfMeasureId  = pol.ContentUnitId
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
    WHERE po.OrderId = @OrderId
      AND (@PurchaseOrderId IS NULL OR pol.PurchaseOrderId = @PurchaseOrderId)
    ORDER BY pol.PurchaseOrderLineId;
END;
GO
