SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_SupplierReturn_GetByToken
(
    @SupplierReturnToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.SupplierReturnId, r.SupplierReturnToken,
        r.PurchaseOrderId, po.PurchaseOrderToken, po.PurchaseOrderNumber,
        po.OrganizationId, po.WarehouseId, po.SupplierId, s.SupplierToken, s.Name AS SupplierName,
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
