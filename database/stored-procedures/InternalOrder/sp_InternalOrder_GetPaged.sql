SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INTERNALORDER - GET PAGED
   Visibility: only the two Organizations party to an InternalOrder may see
   it (the requesting Asociado and the source Asociado), plus an
   unrestricted SuperAdmin (@ContextOrganizationId = NULL). @DirectionFilter
   narrows to just one side — 'REQUESTING' for "my requests",
   'SOURCE' for "requests I need to fulfill" — the frontend's two separate
   tabs/pages. NULL shows both directions.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InternalOrder_GetPaged
(
    @PageNumber              INT,
    @PageSize                INT,
    @ContextOrganizationId   INT          = NULL,
    @DirectionFilter         VARCHAR(20)  = NULL,   -- 'REQUESTING' | 'SOURCE' | NULL (both)
    @Status                  VARCHAR(20)  = NULL
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
        io.CreatedUtc, io.CreatedBy, io.LastUpdatedUtc, io.LastUpdatedBy,
        lc.LineCount,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.InternalOrders io
    JOIN dbo.Organizations reqOrg ON reqOrg.OrganizationId = io.RequestingOrganizationId
    JOIN dbo.Organizations srcOrg ON srcOrg.OrganizationId = io.SourceOrganizationId
    JOIN dbo.Warehouses dw ON dw.WarehouseId = io.DestinationWarehouseId
    JOIN dbo.InternalOrderStatuses ios ON ios.InternalOrderStatusId = io.InternalOrderStatusId
    CROSS APPLY (SELECT COUNT(*) AS LineCount FROM dbo.InternalOrderLines l WHERE l.InternalOrderId = io.InternalOrderId) lc
    WHERE (@ContextOrganizationId IS NULL OR io.RequestingOrganizationId = @ContextOrganizationId OR io.SourceOrganizationId = @ContextOrganizationId)
      AND (@DirectionFilter IS NULL
           OR (@DirectionFilter = 'REQUESTING' AND io.RequestingOrganizationId = @ContextOrganizationId)
           OR (@DirectionFilter = 'SOURCE' AND io.SourceOrganizationId = @ContextOrganizationId))
      AND (@Status IS NULL OR ios.Code = @Status)
    ORDER BY io.CreatedUtc DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
