SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERINVOICE - GET PAGED
   @RootOrganizationId expands via a recursive hierarchy CTE, same shape as
   sp_PurchaseOrder_GetPaged/sp_Order_GetPaged: passing a specific org (leaf
   ASSOCIATE property, no descendants) behaves like the old exact-match
   filter; passing a SUPER_ASSOCIATE org's token or leaving it NULL (bare
   SuperAdmin only, enforced in SupplierInvoiceService) browses every
   descendant/all organizations' invoices at once.
   Totals (TotalTaxableAmount/TotalAmount) and the consolidated PO numbers
   are computed here via subqueries/STRING_AGG rather than stored on the
   header, same "compute on read" convention PurchaseOrder itself uses.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierInvoice_GetPaged
(
    @RootOrganizationId INT          = NULL,
    @SupplierId       INT          = NULL,
    @StatusId         INT          = NULL,
    @SearchText       VARCHAR(200) = NULL,
    @FromDate         DATE         = NULL,
    @ToDate           DATE         = NULL,
    @PageNumber       INT,
    @PageSize         INT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ToDateExclusive DATE = DATEADD(DAY, 1, @ToDate);

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
        si.SupplierInvoiceId, si.SupplierInvoiceToken,
        si.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        si.SupplierId, s.SupplierToken, s.Name AS SupplierName,
        si.SupplierInvoiceNumber, si.InternalSequentialNumber, si.InvoiceDate,
        si.SupplierInvoiceStatusId, sis.Code AS Status,
        si.AttachmentUrl, si.Notes, si.CreatedUtc, si.CreatedBy,
        totals.LineCount, totals.TotalTaxableAmount, totals.TotalAmount,
        pos.PurchaseOrderNumbers,
        whs.WarehouseNames,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.SupplierInvoices si
    JOIN dbo.Organizations org          ON org.OrganizationId = si.OrganizationId
    JOIN dbo.Suppliers s                ON s.SupplierId       = si.SupplierId
    JOIN dbo.SupplierInvoiceStatuses sis ON sis.SupplierInvoiceStatusId = si.SupplierInvoiceStatusId
    CROSS APPLY (
        SELECT COUNT(*) AS LineCount, SUM(sil.TaxableAmount) AS TotalTaxableAmount, SUM(sil.TotalAmount) AS TotalAmount
        FROM dbo.SupplierInvoiceLines sil
        WHERE sil.SupplierInvoiceId = si.SupplierInvoiceId
    ) totals
    CROSS APPLY (
        SELECT STRING_AGG(po.PurchaseOrderNumber, ', ') AS PurchaseOrderNumbers
        FROM dbo.SupplierInvoicePurchaseOrders sipo
        JOIN dbo.PurchaseOrder po ON po.PurchaseOrderId = sipo.PurchaseOrderId
        WHERE sipo.SupplierInvoiceId = si.SupplierInvoiceId
    ) pos
    CROSS APPLY (
        -- STRING_AGG has no DISTINCT support in T-SQL — dedupe via a derived table first.
        SELECT STRING_AGG(dw.Name, ', ') AS WarehouseNames
        FROM (
            SELECT DISTINCT w.Name
            FROM dbo.SupplierInvoiceGoodsReceipts sigr
            JOIN dbo.GoodsReceipt gr ON gr.GoodsReceiptId = sigr.GoodsReceiptId
            JOIN dbo.Warehouses w    ON w.WarehouseId     = gr.WarehouseId
            WHERE sigr.SupplierInvoiceId = si.SupplierInvoiceId
        ) dw
    ) whs
    WHERE (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = si.OrganizationId))
      AND (@SupplierId IS NULL OR si.SupplierId = @SupplierId)
      AND (@StatusId IS NULL OR si.SupplierInvoiceStatusId = @StatusId)
      AND (@SearchText IS NULL
           OR LOWER(si.SupplierInvoiceNumber) LIKE '%' + LOWER(@SearchText) + '%'
           OR LOWER(si.InternalSequentialNumber) LIKE '%' + LOWER(@SearchText) + '%')
      AND (@FromDate IS NULL OR si.InvoiceDate >= @FromDate)
      AND (@ToDate IS NULL OR si.InvoiceDate < @ToDateExclusive)
    ORDER BY si.CreatedUtc DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO
