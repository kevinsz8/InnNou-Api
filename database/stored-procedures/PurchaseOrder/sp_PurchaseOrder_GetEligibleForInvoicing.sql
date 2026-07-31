SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PURCHASEORDER - GET ELIGIBLE FOR INVOICING
   Feeds the SupplierInvoice creation picker: RECEIVED (100%) PurchaseOrders
   for one Organization + Supplier, excluding any already consolidated into
   an existing SupplierInvoice (enforced by
   UX_SupplierInvoicePurchaseOrders_PurchaseOrderId at the DB level, but
   filtered here too so the picker never even shows an ineligible PO).
   No pagination — same "small, bounded picker list" shape as
   Order/OrderTemplate's article-search modal.

   Added search filters (2026-08-01): @PurchaseOrderNumber and
   @DeliveryNoteNumber are independent LIKE filters — the delivery note
   number lives on GoodsReceipt (one PO can have several receipts, each
   with its own note), never on PurchaseOrder itself, so it's an EXISTS
   join, not a column on po. @DateType selects which date the
   @FromDate/@ToDate range applies to: ORDER_DATE filters po.SentUtc (when
   the PO was sent to the supplier); RECEIPT_DATE filters against ANY of
   the PO's GoodsReceipt.CreatedUtc rows (GoodsReceipt has no separate
   "received date" column — CreatedUtc is the receipt date, same column
   sp_GoodsReceipt_GetPaged already orders by).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrder_GetEligibleForInvoicing
(
    @OrganizationId      INT,
    @SupplierId          INT,
    @PurchaseOrderNumber VARCHAR(50)  = NULL,
    @DeliveryNoteNumber  NVARCHAR(100) = NULL,
    @FromDate            DATE         = NULL,
    @ToDate              DATE         = NULL,
    @DateType            VARCHAR(20)  = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ToDateExclusive DATETIME2 = DATEADD(DAY, 1, @ToDate);

    SELECT
        po.PurchaseOrderId, po.PurchaseOrderToken, po.PurchaseOrderNumber,
        po.SupplierId, s.Name AS SupplierName,
        po.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        po.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        pos.Code AS Status, po.SentUtc,
        po.CreatedUtc, po.CreatedBy,
        lc.LineCount
    FROM dbo.PurchaseOrder po
    JOIN dbo.Suppliers s               ON s.SupplierId       = po.SupplierId
    JOIN dbo.Organizations org         ON org.OrganizationId = po.OrganizationId
    JOIN dbo.Warehouses w              ON w.WarehouseId      = po.WarehouseId
    JOIN dbo.PurchaseOrderStatuses pos ON pos.PurchaseOrderStatusId = po.PurchaseOrderStatusId
    CROSS APPLY (SELECT COUNT(*) AS LineCount FROM dbo.PurchaseOrderLine pol WHERE pol.PurchaseOrderId = po.PurchaseOrderId) lc
    WHERE po.OrganizationId = @OrganizationId
      AND po.SupplierId = @SupplierId
      AND pos.Code = 'RECEIVED'
      AND NOT EXISTS (SELECT 1 FROM dbo.SupplierInvoicePurchaseOrders sipo WHERE sipo.PurchaseOrderId = po.PurchaseOrderId)
      AND (@PurchaseOrderNumber IS NULL OR po.PurchaseOrderNumber LIKE '%' + @PurchaseOrderNumber + '%')
      AND (@DeliveryNoteNumber IS NULL OR EXISTS (
          SELECT 1 FROM dbo.GoodsReceipt gr
          WHERE gr.PurchaseOrderId = po.PurchaseOrderId
            AND gr.DeliveryNoteNumber LIKE '%' + @DeliveryNoteNumber + '%'
      ))
      AND (
          @FromDate IS NULL AND @ToDate IS NULL
          OR (
              @DateType = 'RECEIPT_DATE' AND EXISTS (
                  SELECT 1 FROM dbo.GoodsReceipt gr
                  WHERE gr.PurchaseOrderId = po.PurchaseOrderId
                    AND (@FromDate IS NULL OR gr.CreatedUtc >= @FromDate)
                    AND (@ToDate IS NULL OR gr.CreatedUtc < @ToDateExclusive)
              )
          )
          OR (
              (@DateType IS NULL OR @DateType = 'ORDER_DATE')
              AND (@FromDate IS NULL OR po.SentUtc >= @FromDate)
              AND (@ToDate IS NULL OR po.SentUtc < @ToDateExclusive)
          )
      )
    ORDER BY po.CreatedUtc DESC;
END;
GO
