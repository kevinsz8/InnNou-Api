SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   GOODSRECEIPT - GET ELIGIBLE FOR INVOICING
   Supersedes sp_PurchaseOrder_GetEligibleForInvoicing (2026-08-02) — invoice
   creation moved from whole-PurchaseOrder granularity to individual-
   GoodsReceipt granularity ("Goods-Receipt-Based Invoice Verification",
   same shape SAP uses): a PurchaseOrder can receive several partial
   deliveries over time, each independently invoiceable as soon as it
   arrives, without waiting for the rest of the order.

   One row per eligible GoodsReceipt (not per PurchaseOrder) — a PO with 3
   receipts shows as 3 rows. Eligible = the owning PO is at least
   PARTIALLY_RECEIVED (not just fully RECEIVED — a partial delivery is
   invoiceable the moment it arrives), the receipt has at least one
   billable line (QuantityAccepted > 0), and the receipt itself hasn't
   already been invoiced (NOT EXISTS in SupplierInvoiceGoodsReceipts — the
   new exclusivity gate, replacing the old PurchaseOrder-level one).

   @DeliveryNoteNumber matches this receipt's own note (a plain column
   match now, not an EXISTS join — unlike the old PO-level filter, each row
   here already IS a single receipt). @DateType selects which date
   @FromDate/@ToDate filters: ORDER_DATE = the owning PO's SentUtc,
   RECEIPT_DATE = this receipt's own CreatedUtc.

   Per-row TotalTaxableAmount/TotalAmount are THIS RECEIPT's own totals
   (SUM of its GoodsReceiptLine rows, tax already frozen at receipt time —
   see .claude/GoodsReceiptsModule.md's Facturacion section), not the whole
   PO's — summing the rows of one PO gives that PO's total.

   Paginated (OFFSET/FETCH), same shape as sp_GoodsReceipt_GetPaged.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_GoodsReceipt_GetEligibleForInvoicing
(
    @OrganizationId      INT,
    @SupplierId          INT,
    @PurchaseOrderNumber VARCHAR(50)   = NULL,
    @DeliveryNoteNumber  NVARCHAR(100) = NULL,
    @FromDate            DATE          = NULL,
    @ToDate              DATE          = NULL,
    @DateType            VARCHAR(20)   = NULL,
    @PageNumber          INT,
    @PageSize            INT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ToDateExclusive DATETIME2 = DATEADD(DAY, 1, @ToDate);

    SELECT
        gr.GoodsReceiptId, gr.GoodsReceiptToken,
        po.PurchaseOrderId, po.PurchaseOrderToken, po.PurchaseOrderNumber, po.SentUtc AS PurchaseOrderSentUtc,
        pos.Code AS PurchaseOrderStatus,
        gr.DeliveryNoteNumber, gr.CreatedUtc AS GoodsReceiptCreatedUtc,
        gr.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        COALESCE(totals.TotalTaxableAmount, 0) AS TotalTaxableAmount,
        COALESCE(totals.TotalAmount, 0)        AS TotalAmount,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.GoodsReceipt gr
    JOIN dbo.PurchaseOrder po           ON po.PurchaseOrderId = gr.PurchaseOrderId
    JOIN dbo.PurchaseOrderStatuses pos  ON pos.PurchaseOrderStatusId = po.PurchaseOrderStatusId
    JOIN dbo.Warehouses w               ON w.WarehouseId      = gr.WarehouseId
    CROSS APPLY (
        SELECT SUM(grl.TaxableAmount) AS TotalTaxableAmount, SUM(grl.TotalAmount) AS TotalAmount
        FROM dbo.GoodsReceiptLine grl
        WHERE grl.GoodsReceiptId = gr.GoodsReceiptId AND grl.QuantityAccepted > 0
    ) totals
    WHERE po.OrganizationId = @OrganizationId
      AND po.SupplierId = @SupplierId
      AND pos.Code IN ('PARTIALLY_RECEIVED', 'RECEIVED')
      AND EXISTS (SELECT 1 FROM dbo.GoodsReceiptLine grl WHERE grl.GoodsReceiptId = gr.GoodsReceiptId AND grl.QuantityAccepted > 0)
      AND NOT EXISTS (SELECT 1 FROM dbo.SupplierInvoiceGoodsReceipts sigr WHERE sigr.GoodsReceiptId = gr.GoodsReceiptId)
      AND (@PurchaseOrderNumber IS NULL OR po.PurchaseOrderNumber LIKE '%' + @PurchaseOrderNumber + '%')
      AND (@DeliveryNoteNumber IS NULL OR gr.DeliveryNoteNumber LIKE '%' + @DeliveryNoteNumber + '%')
      AND (
          @FromDate IS NULL AND @ToDate IS NULL
          OR (
              @DateType = 'RECEIPT_DATE'
              AND (@FromDate IS NULL OR gr.CreatedUtc >= @FromDate)
              AND (@ToDate IS NULL OR gr.CreatedUtc < @ToDateExclusive)
          )
          OR (
              (@DateType IS NULL OR @DateType = 'ORDER_DATE')
              AND (@FromDate IS NULL OR po.SentUtc >= @FromDate)
              AND (@ToDate IS NULL OR po.SentUtc < @ToDateExclusive)
          )
      )
    ORDER BY gr.CreatedUtc DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
