SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   REQUISITION - GET PAGED
   @RootOrganizationId scopes by hierarchy (NULL = unrestricted, SuperAdmin
   only); @WarehouseId/@DepartmentId/@StatusId/@FromDate/@ToDate are all
   optional narrowing filters layered on top, same additive-AND-on-top-of-
   scope shape as every other GetPaged in this codebase. Dates filter on
   CreatedUtc, inclusive both ends.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Requisition_GetPaged
(
    @RootOrganizationId INT          = NULL,
    @WarehouseId        INT          = NULL,
    @DepartmentId       INT          = NULL,
    @StatusId           INT          = NULL,
    @FromDate           DATE         = NULL,
    @ToDate             DATE         = NULL,
    @PageNumber         INT,
    @PageSize           INT
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
        r.RequisitionId, r.RequisitionToken, r.RequisitionNumber,
        r.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        r.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        r.DepartmentId, d.DepartmentToken, d.Name AS DepartmentName,
        rs.Code AS Status,
        r.Notes,
        r.ApprovedUtc, r.ApprovedBy,
        r.RejectedUtc, r.RejectedBy, r.RejectedReason,
        r.CancelledUtc, r.CancelledBy, r.CancelledReason,
        r.ClosedShortUtc, r.ClosedShortBy, r.ClosedShortReason,
        r.CreatedUtc, r.CreatedBy, r.LastUpdatedUtc, r.LastUpdatedBy,
        lc.LineCount,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.Requisitions r
    JOIN dbo.Organizations org ON org.OrganizationId = r.OrganizationId
    JOIN dbo.Warehouses w      ON w.WarehouseId      = r.WarehouseId
    JOIN dbo.Departments d     ON d.DepartmentId     = r.DepartmentId
    JOIN dbo.RequisitionStatuses rs ON rs.RequisitionStatusId = r.RequisitionStatusId
    CROSS APPLY (SELECT COUNT(*) AS LineCount FROM dbo.RequisitionLines rl WHERE rl.RequisitionId = r.RequisitionId) lc
    WHERE
        (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = r.OrganizationId))
        AND (@WarehouseId IS NULL OR r.WarehouseId = @WarehouseId)
        AND (@DepartmentId IS NULL OR r.DepartmentId = @DepartmentId)
        AND (@StatusId IS NULL OR r.RequisitionStatusId = @StatusId)
        AND (@FromDate IS NULL OR r.CreatedUtc >= @FromDate)
        AND (@ToDateExclusive IS NULL OR r.CreatedUtc < @ToDateExclusive)
    ORDER BY r.CreatedUtc DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
