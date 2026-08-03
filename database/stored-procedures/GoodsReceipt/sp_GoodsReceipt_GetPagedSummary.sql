SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   GOODSRECEIPT - GET PAGED SUMMARY
   Backs the standalone "Recepciones" history/search page — unlike
   sp_GoodsReceipt_GetPaged (always scoped to one PurchaseOrder, eagerly
   hydrates every row's Lines from the caller side since the result set is
   inherently small there), this is a genuinely unbounded cross-organization
   browse, so it stays a single flat query with no per-row round trip — same
   denormalized-summary-row shape as sp_GoodsReceipt_GetEligibleForInvoicing,
   just without that procedure's invoicing-eligibility restrictions (PO
   status IN ('PARTIALLY_RECEIVED','RECEIVED'), not-yet-invoiced, mandatory
   Supplier) — this lists every receipt ever recorded, regardless of
   invoicing state.

   Same visibility shape as sp_GoodsReceipt_GetPaged/sp_PurchaseOrder_GetPaged
   — @SupplierId set scopes to the owning supplier's own receipts; otherwise
   the organization-hierarchy branch applies (@RootOrganizationId = NULL is
   unrestricted, SuperAdmin only). @FromDate/@ToDate filter the receipt's own
   CreatedUtc (when it was recorded), not the owning PO's SentUtc.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_GoodsReceipt_GetPagedSummary
(
    @RootOrganizationId  INT           = NULL,
    @SupplierId          INT           = NULL,
    @WarehouseId         INT           = NULL,
    @PurchaseOrderNumber VARCHAR(50)   = NULL,
    @DeliveryNoteNumber  NVARCHAR(100) = NULL,
    @FromDate            DATE          = NULL,
    @ToDate              DATE          = NULL,
    @PageNumber          INT,
    @PageSize            INT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ToDateExclusive DATETIME2 = DATEADD(DAY, 1, @ToDate);

    ;WITH OrganizationHierarchy AS
    (
        SELECT o.OrganizationId
        FROM dbo.Organizations o
        WHERE o.OrganizationId = @RootOrganizationId

        UNION ALL

        SELECT o.OrganizationId
        FROM dbo.Organizations o
        INNER JOIN OrganizationHierarchy oh ON o.ParentOrganizationId = oh.OrganizationId
    )
    SELECT
        gr.GoodsReceiptId, gr.GoodsReceiptToken,
        po.PurchaseOrderId, po.PurchaseOrderToken, po.PurchaseOrderNumber, po.SentUtc AS PurchaseOrderSentUtc,
        pos.Code AS PurchaseOrderStatus,
        s.SupplierId, s.Name AS SupplierName,
        gr.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        gr.DeliveryNoteNumber, gr.DeliveryNoteDate, gr.CreatedUtc AS GoodsReceiptCreatedUtc, gr.CreatedBy,
        COALESCE(totals.TotalTaxableAmount, 0) AS TotalTaxableAmount,
        COALESCE(totals.TotalAmount, 0)        AS TotalAmount,
        lc.LineCount,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.GoodsReceipt gr
    JOIN dbo.PurchaseOrder po          ON po.PurchaseOrderId = gr.PurchaseOrderId
    JOIN dbo.PurchaseOrderStatuses pos ON pos.PurchaseOrderStatusId = po.PurchaseOrderStatusId
    JOIN dbo.Suppliers s               ON s.SupplierId = po.SupplierId
    JOIN dbo.Warehouses w              ON w.WarehouseId = gr.WarehouseId
    CROSS APPLY (SELECT COUNT(*) AS LineCount FROM dbo.GoodsReceiptLine grl WHERE grl.GoodsReceiptId = gr.GoodsReceiptId) lc
    CROSS APPLY (
        SELECT SUM(grl.TaxableAmount) AS TotalTaxableAmount, SUM(grl.TotalAmount) AS TotalAmount
        FROM dbo.GoodsReceiptLine grl
        WHERE grl.GoodsReceiptId = gr.GoodsReceiptId AND grl.QuantityAccepted > 0
    ) totals
    WHERE
        (
            (@SupplierId IS NOT NULL AND po.SupplierId = @SupplierId)
            OR (@SupplierId IS NULL AND (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = po.OrganizationId)))
        )
        AND (@WarehouseId IS NULL OR gr.WarehouseId = @WarehouseId)
        AND (@PurchaseOrderNumber IS NULL OR po.PurchaseOrderNumber LIKE '%' + @PurchaseOrderNumber + '%')
        AND (@DeliveryNoteNumber IS NULL OR gr.DeliveryNoteNumber LIKE '%' + @DeliveryNoteNumber + '%')
        AND (@FromDate IS NULL OR gr.CreatedUtc >= @FromDate)
        AND (@ToDate IS NULL OR gr.CreatedUtc < @ToDateExclusive)
    ORDER BY gr.CreatedUtc DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
