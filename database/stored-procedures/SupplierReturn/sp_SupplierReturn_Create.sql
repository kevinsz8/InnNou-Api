SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERRETURN - CREATE
   Header only — SupplierReturnLine rows are inserted separately by the
   caller inside the same transaction (sp_SupplierReturnLine_Create), one
   per claimed GoodsReceiptLine, same shape as PurchaseOrderRectifications/
   PurchaseOrderLineRectifications.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierReturn_Create
(
    @SupplierReturnToken UNIQUEIDENTIFIER,
    @PurchaseOrderId     INT,
    @Notes               NVARCHAR(500) = NULL,
    @CreatedBy           VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.SupplierReturns
        (SupplierReturnToken, PurchaseOrderId, SupplierReturnStatusId, Notes, CreatedBy)
    VALUES
        (@SupplierReturnToken, @PurchaseOrderId,
         (SELECT SupplierReturnStatusId FROM dbo.SupplierReturnStatuses WHERE Code = 'PENDING'),
         @Notes, @CreatedBy);

    SELECT
        r.SupplierReturnId, r.SupplierReturnToken,
        r.PurchaseOrderId, po.PurchaseOrderToken, po.PurchaseOrderNumber,
        po.OrganizationId, po.SupplierId, s.SupplierToken, s.Name AS SupplierName,
        statuses.Code AS Status,
        resolutionTypes.Code AS ResolutionType,
        r.Notes, r.ClosedUtc, r.ClosedBy, r.CreatedUtc, r.CreatedBy
    FROM dbo.SupplierReturns r
    JOIN dbo.PurchaseOrder po ON po.PurchaseOrderId = r.PurchaseOrderId
    JOIN dbo.Suppliers s ON s.SupplierId = po.SupplierId
    JOIN dbo.SupplierReturnStatuses statuses ON statuses.SupplierReturnStatusId = r.SupplierReturnStatusId
    LEFT JOIN dbo.SupplierReturnResolutionTypes resolutionTypes ON resolutionTypes.SupplierReturnResolutionTypeId = r.SupplierReturnResolutionTypeId
    WHERE r.SupplierReturnToken = @SupplierReturnToken;
END;
GO
