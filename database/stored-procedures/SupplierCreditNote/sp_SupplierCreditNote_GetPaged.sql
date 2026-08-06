SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERCREDITNOTE - GET PAGED
   Same shape as sp_SupplierReturn_GetPaged (buyer-side only, additive
   @RootOrganizationId hierarchy filter, @RestrictToWarehouseId for a
   WarehouseContact login).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierCreditNote_GetPaged
(
    @RootOrganizationId  INT          = NULL,
    @SupplierId          INT          = NULL,
    @FromDate            DATE         = NULL,
    @ToDate              DATE         = NULL,
    @PurchaseOrderNumber VARCHAR(20)  = NULL,
    @PageNumber          INT,
    @PageSize            INT,
    @RestrictToWarehouseId INT        = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ToDateExclusive DATETIME2 = CASE WHEN @ToDate IS NULL THEN NULL ELSE DATEADD(DAY, 1, CAST(@ToDate AS DATETIME2)) END;

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
        scn.SupplierCreditNoteId, scn.SupplierCreditNoteToken,
        scn.SupplierReturnId, sr.SupplierReturnToken,
        sr.PurchaseOrderId, po.PurchaseOrderToken, po.PurchaseOrderNumber, po.WarehouseId,
        scn.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        scn.SupplierId, s.SupplierToken, s.Name AS SupplierName,
        scn.CreditNoteNumber, scn.InternalSequentialNumber, scn.CreditNoteDate, scn.Reason, scn.Notes,
        scn.CreatedUtc, scn.CreatedBy,
        lc.LineCount,
        tot.TotalAmount,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.SupplierCreditNotes scn
    JOIN dbo.SupplierReturns sr ON sr.SupplierReturnId = scn.SupplierReturnId
    JOIN dbo.PurchaseOrder po   ON po.PurchaseOrderId  = sr.PurchaseOrderId
    JOIN dbo.Organizations org  ON org.OrganizationId  = scn.OrganizationId
    JOIN dbo.Suppliers s        ON s.SupplierId        = scn.SupplierId
    CROSS APPLY (SELECT COUNT(*) AS LineCount FROM dbo.SupplierCreditNoteLines l WHERE l.SupplierCreditNoteId = scn.SupplierCreditNoteId) lc
    CROSS APPLY (SELECT ISNULL(SUM(l.TotalAmount), 0) AS TotalAmount FROM dbo.SupplierCreditNoteLines l WHERE l.SupplierCreditNoteId = scn.SupplierCreditNoteId) tot
    WHERE
        (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = scn.OrganizationId))
        AND (@SupplierId IS NULL OR scn.SupplierId = @SupplierId)
        AND (@FromDate IS NULL OR scn.CreatedUtc >= @FromDate)
        AND (@ToDateExclusive IS NULL OR scn.CreatedUtc < @ToDateExclusive)
        AND (@PurchaseOrderNumber IS NULL OR LOWER(po.PurchaseOrderNumber) LIKE '%' + LOWER(@PurchaseOrderNumber) + '%')
        AND (@RestrictToWarehouseId IS NULL OR po.WarehouseId = @RestrictToWarehouseId)
    ORDER BY scn.CreatedUtc DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
