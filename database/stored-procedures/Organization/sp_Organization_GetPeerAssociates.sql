SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORGANIZATION - GET PEER ASSOCIATES
   Every other ASSOCIATE-typed organization descending from @OrganizationId's own nearest
   SUPER_ASSOCIATE ancestor (siblings, cousins, etc.) — the exact "same Super Asociado" scope an
   Internal Order's source organization must fall within. Distinct from sp_Organization_GetPaged's
   own @RootOrganizationId scoping, which only ever returns @OrganizationId's own DESCENDANTS,
   never siblings — the reason this dedicated lookup exists at all. Excludes @OrganizationId
   itself. Returns nothing if @OrganizationId has no SUPER_ASSOCIATE ancestor.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Organization_GetPeerAssociates
(
    @OrganizationId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SuperAssociateOrganizationId INT;

    ;WITH OrganizationAncestry AS
    (
        SELECT OrganizationId, ParentOrganizationId, OrganizationTypeId, 0 AS Depth
        FROM dbo.Organizations
        WHERE OrganizationId = @OrganizationId AND IsDeleted = 0 AND IsActive = 1

        UNION ALL

        SELECT o.OrganizationId, o.ParentOrganizationId, o.OrganizationTypeId, oa.Depth + 1
        FROM dbo.Organizations o
        INNER JOIN OrganizationAncestry oa ON o.OrganizationId = oa.ParentOrganizationId
        WHERE o.IsDeleted = 0 AND o.IsActive = 1
    )
    SELECT TOP 1 @SuperAssociateOrganizationId = oa.OrganizationId
    FROM OrganizationAncestry oa
    JOIN dbo.OrganizationTypes ot ON ot.OrganizationTypeId = oa.OrganizationTypeId
    WHERE ot.Code = 'SUPER_ASSOCIATE'
    ORDER BY oa.Depth ASC;

    IF @SuperAssociateOrganizationId IS NULL
        RETURN;

    ;WITH Descendants AS
    (
        SELECT OrganizationId
        FROM dbo.Organizations
        WHERE OrganizationId = @SuperAssociateOrganizationId AND IsDeleted = 0 AND IsActive = 1

        UNION ALL

        SELECT o.OrganizationId
        FROM dbo.Organizations o
        INNER JOIN Descendants d ON o.ParentOrganizationId = d.OrganizationId
        WHERE o.IsDeleted = 0 AND o.IsActive = 1
    )
    SELECT
        o.OrganizationId, o.OrganizationToken, o.Name, o.NormalizedName, o.Code,
        o.ParentOrganizationId, o.OrganizationTypeId, ot.Code AS OrganizationTypeCode,
        o.IsActive, o.IsDeleted
    FROM dbo.Organizations o
    JOIN dbo.OrganizationTypes ot ON ot.OrganizationTypeId = o.OrganizationTypeId
    JOIN Descendants d ON d.OrganizationId = o.OrganizationId
    WHERE ot.Code = 'ASSOCIATE'
      AND o.OrganizationId <> @OrganizationId
    ORDER BY o.Name;
END;
GO
