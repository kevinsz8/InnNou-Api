SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   DASHBOARD - GET ACTIVE USER SUMMARY
   One row: count of active users + count of distinct organizations they
   belong to, scoped to the caller's own hierarchy. COUNT(DISTINCT
   OrganizationId) naturally ignores NULL (shadow/unassigned users), no
   extra filtering needed for that column.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Dashboard_GetActiveUserSummary
(
    @RootOrganizationId INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

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
        COUNT(*) AS ActiveUserCount,
        COUNT(DISTINCT u.OrganizationId) AS ActiveOrganizationCount
    FROM dbo.Users u
    WHERE u.IsActive = 1
      AND u.IsDeleted = 0
      AND (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = u.OrganizationId));
END;
GO
