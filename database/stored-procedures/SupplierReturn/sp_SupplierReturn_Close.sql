SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERRETURN - CLOSE
   The service layer already validated the case is currently PENDING
   before calling this — the @@ROWCOUNT check below is a defense-in-depth
   backstop against a concurrent double-close race, not the primary gate.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierReturn_Close
(
    @SupplierReturnId INT,
    @ResolutionType   VARCHAR(20),
    @Notes            NVARCHAR(500) = NULL,
    @ClosedUtc        DATETIME2,
    @ClosedBy         VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SupplierReturns
    SET
        SupplierReturnStatusId = (SELECT SupplierReturnStatusId FROM dbo.SupplierReturnStatuses WHERE Code = 'CLOSED'),
        SupplierReturnResolutionTypeId = (SELECT SupplierReturnResolutionTypeId FROM dbo.SupplierReturnResolutionTypes WHERE Code = @ResolutionType),
        Notes = COALESCE(@Notes, Notes),
        ClosedUtc = @ClosedUtc,
        ClosedBy = @ClosedBy
    WHERE SupplierReturnId = @SupplierReturnId
      AND SupplierReturnStatusId = (SELECT SupplierReturnStatusId FROM dbo.SupplierReturnStatuses WHERE Code = 'PENDING');

    IF @@ROWCOUNT = 0
        RETURN;

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
    WHERE r.SupplierReturnId = @SupplierReturnId;
END;
GO
