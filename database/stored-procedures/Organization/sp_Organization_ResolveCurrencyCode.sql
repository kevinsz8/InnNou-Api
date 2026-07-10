SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORGANIZATION - RESOLVE CURRENCY CODE
   Walks UPWARD from @OrganizationId through ParentOrganizationId
   and returns the nearest non-null CurrencyCode (the organization's
   own first, then its parent, grandparent, etc.). Returns NULL if
   nothing in the chain up to the root has a CurrencyCode set.
   Inverse direction of sp_Organization_IsInHierarchy, which only
   walks downward (root -> descendants).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Organization_ResolveCurrencyCode
(
    @OrganizationId INT,
    @CurrencyCode   VARCHAR(10) = NULL OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH OrganizationAncestry AS
    (
        SELECT OrganizationId, ParentOrganizationId, CurrencyCode, 0 AS Depth
        FROM   dbo.Organizations
        WHERE  OrganizationId = @OrganizationId
          AND  IsDeleted = 0
          AND  IsActive  = 1

        UNION ALL

        SELECT o.OrganizationId, o.ParentOrganizationId, o.CurrencyCode, oa.Depth + 1
        FROM   dbo.Organizations o
        INNER JOIN OrganizationAncestry oa ON o.OrganizationId = oa.ParentOrganizationId
        WHERE  o.IsDeleted = 0
          AND  o.IsActive  = 1
    )
    SELECT TOP 1 @CurrencyCode = CurrencyCode
    FROM   OrganizationAncestry
    WHERE  CurrencyCode IS NOT NULL
    ORDER BY Depth ASC;
END;
GO
