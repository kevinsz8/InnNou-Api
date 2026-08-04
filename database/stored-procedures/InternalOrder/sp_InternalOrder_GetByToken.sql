SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INTERNALORDER - GET BY TOKEN
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InternalOrder_GetByToken
(
    @InternalOrderToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        io.InternalOrderId, io.InternalOrderToken, io.InternalOrderNumber,
        io.RequestingOrganizationId, reqOrg.OrganizationToken AS RequestingOrganizationToken, reqOrg.Name AS RequestingOrganizationName,
        io.SourceOrganizationId, srcOrg.OrganizationToken AS SourceOrganizationToken, srcOrg.Name AS SourceOrganizationName,
        io.DestinationWarehouseId, dw.WarehouseToken AS DestinationWarehouseToken, dw.Name AS DestinationWarehouseName,
        ios.Code AS Status,
        io.Notes,
        io.CancelledUtc, io.CancelledBy, io.CancelledReason,
        io.CreatedUtc, io.CreatedBy, io.LastUpdatedUtc, io.LastUpdatedBy
    FROM dbo.InternalOrders io
    JOIN dbo.Organizations reqOrg ON reqOrg.OrganizationId = io.RequestingOrganizationId
    JOIN dbo.Organizations srcOrg ON srcOrg.OrganizationId = io.SourceOrganizationId
    JOIN dbo.Warehouses dw ON dw.WarehouseId = io.DestinationWarehouseId
    JOIN dbo.InternalOrderStatuses ios ON ios.InternalOrderStatusId = io.InternalOrderStatusId
    WHERE io.InternalOrderToken = @InternalOrderToken;
END;
GO
