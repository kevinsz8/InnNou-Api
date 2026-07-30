SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERINVOICE - GET PAGED
   Always scoped to one Organization (no cross-tenant browse — an invoice
   belongs to exactly one buying Organization, same as PurchaseOrder).
   Totals (TotalTaxableAmount/TotalAmount) and the consolidated PO numbers
   are computed here via subqueries/STRING_AGG rather than stored on the
   header, same "compute on read" convention PurchaseOrder itself uses.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierInvoice_GetPaged
(
    @OrganizationId   INT,
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

    SELECT
        si.SupplierInvoiceId, si.SupplierInvoiceToken,
        si.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        si.SupplierId, s.SupplierToken, s.Name AS SupplierName,
        si.SupplierInvoiceNumber, si.InternalSequentialNumber, si.InvoiceDate,
        si.SupplierInvoiceStatusId, sis.Code AS Status,
        si.AttachmentUrl, si.Notes, si.CreatedUtc, si.CreatedBy,
        totals.LineCount, totals.TotalTaxableAmount, totals.TotalAmount,
        pos.PurchaseOrderNumbers,
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
    WHERE si.OrganizationId = @OrganizationId
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
