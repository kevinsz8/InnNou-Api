SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORGANIZATION - IS IN HIERARCHY
   Returns 1 if @TargetOrganizationId is within the subtree rooted
   at @RootOrganizationId, 0 otherwise. Used for authorization
   checks.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Organization_IsInHierarchy
(
    @RootOrganizationId   INT,
    @TargetOrganizationId INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @TargetOrganizationId IS NULL
    BEGIN
        SELECT 0;
        RETURN;
    END;

    ;WITH OrganizationHierarchy AS
    (
        SELECT OrganizationId, ParentOrganizationId
        FROM dbo.Organizations
        WHERE OrganizationId = @RootOrganizationId
          AND IsDeleted = 0
          AND IsActive  = 1

        UNION ALL

        SELECT o.OrganizationId, o.ParentOrganizationId
        FROM dbo.Organizations o
        INNER JOIN OrganizationHierarchy oh ON o.ParentOrganizationId = oh.OrganizationId
        WHERE o.IsDeleted = 0
          AND o.IsActive  = 1
    )
    SELECT
        CASE
            WHEN EXISTS
            (
                SELECT 1 FROM OrganizationHierarchy WHERE OrganizationId = @TargetOrganizationId
            )
            THEN 1
            ELSE 0
        END;
END;
GO
