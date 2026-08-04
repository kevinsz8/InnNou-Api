SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORGANIZATION - GET NEAREST SUPER ASSOCIATE ANCESTOR
   Ascending walk (including @OrganizationId itself, Depth 0) to the nearest
   SUPER_ASSOCIATE-typed ancestor — the same CTE shape inlined in
   sp_Category_GetByToken/sp_Category_GetPaged/sp_SubCategory_* for ownership
   visibility, extracted here as a reusable single-value lookup so
   InternalOrderService can confirm two different Asociado Organizations
   share the same Super Asociado (InnNou's "same corporate group" boundary)
   before letting an Internal Order be created between them. Returns NULL if
   no SUPER_ASSOCIATE ancestor exists.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Organization_GetNearestSuperAssociateAncestor
(
    @OrganizationId INT
)
AS
BEGIN
    SET NOCOUNT ON;

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
    SELECT TOP 1 oa.OrganizationId AS SuperAssociateOrganizationId
    FROM OrganizationAncestry oa
    JOIN dbo.OrganizationTypes ot ON ot.OrganizationTypeId = oa.OrganizationTypeId
    WHERE ot.Code = 'SUPER_ASSOCIATE'
    ORDER BY oa.Depth ASC;
END;
GO
