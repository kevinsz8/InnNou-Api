CREATE OR ALTER PROCEDURE sp_Category_GetByToken
    @CategoryToken         uniqueidentifier,
    @ContextRoleLevel      INT = 100,
    @ContextOrganizationId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CallerSuperAssociateOrganizationId INT = NULL;

    IF @ContextRoleLevel < 100 AND @ContextOrganizationId IS NOT NULL
    BEGIN
        ;WITH OrganizationAncestry AS
        (
            SELECT OrganizationId, ParentOrganizationId, OrganizationTypeId, 0 AS Depth
            FROM dbo.Organizations
            WHERE OrganizationId = @ContextOrganizationId AND IsDeleted = 0 AND IsActive = 1

            UNION ALL

            SELECT o.OrganizationId, o.ParentOrganizationId, o.OrganizationTypeId, oa.Depth + 1
            FROM dbo.Organizations o
            INNER JOIN OrganizationAncestry oa ON o.OrganizationId = oa.ParentOrganizationId
            WHERE o.IsDeleted = 0 AND o.IsActive = 1
        )
        SELECT TOP 1 @CallerSuperAssociateOrganizationId = oa.OrganizationId
        FROM OrganizationAncestry oa
        JOIN dbo.OrganizationTypes ot ON ot.OrganizationTypeId = oa.OrganizationTypeId
        WHERE ot.Code = 'SUPER_ASSOCIATE'
        ORDER BY oa.Depth ASC;
    END

    SELECT
        c.CategoryId,
        c.CategoryToken,
        c.Code,
        c.OrganizationId,
        c.IsSystem,
        c.IsActive,
        c.CreatedUtc,
        c.CreatedBy,
        c.LastUpdatedUtc,
        c.LastUpdatedBy,
        o.OrganizationToken AS OrganizationTokenResult,
        o.Name AS OrganizationName
    FROM Categories c
    LEFT JOIN Organizations o ON o.OrganizationId = c.OrganizationId
    WHERE c.CategoryToken = @CategoryToken
      AND
      (
          @ContextRoleLevel >= 100
          OR c.OrganizationId IS NULL
          OR (@CallerSuperAssociateOrganizationId IS NOT NULL AND c.OrganizationId = @CallerSuperAssociateOrganizationId)
      );
END;
GO
