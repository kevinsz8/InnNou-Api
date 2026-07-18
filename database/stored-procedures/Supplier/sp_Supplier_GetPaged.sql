/* =============================================================
   SUPPLIER - GET PAGED
   Visibility (see InnNou-Api CLAUDE.md, "Supplier global/private
   scoping"): SuperAdmin (RoleLevel >= 100) sees everything. A
   supplier-scoped caller sees only its own supplier row. Everyone
   else (Admin included — Admin is deliberately NO LONGER globally
   unrestricted here) sees global suppliers plus private suppliers
   owned by any organization in their own DESCENDING hierarchy
   (self-or-descendant of @ContextOrganizationId), via the
   OrganizationDescendants CTE below — same shape as
   sp_Organization_IsInHierarchy's own body, NOT the ascending
   OrganizationAncestry CTE used elsewhere for ArticleFavorites
   (that solves the opposite direction: favorites cascade down from
   an ancestor to descendants, so a descendant queries up; here a
   private supplier's visibility cascades up from the owning
   descendant to its ancestors, so a viewer queries down from
   itself).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Supplier_GetPaged
(
    @ContextRoleLevel      INT,
    @ContextSupplierId     INT          = NULL,
    @ContextOrganizationId INT          = NULL,
    @SearchField           VARCHAR(50)  = NULL,
    @SearchText            VARCHAR(200) = NULL,
    @PageNumber            INT,
    @PageSize              INT,
    @IncludeInactive       BIT          = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH OrganizationDescendants AS
    (
        SELECT OrganizationId, ParentOrganizationId
        FROM dbo.Organizations
        WHERE OrganizationId = @ContextOrganizationId
          AND IsDeleted = 0
          AND IsActive  = 1

        UNION ALL

        SELECT o.OrganizationId, o.ParentOrganizationId
        FROM dbo.Organizations o
        INNER JOIN OrganizationDescendants od ON o.ParentOrganizationId = od.OrganizationId
        WHERE o.IsDeleted = 0
          AND o.IsActive  = 1
    )
    SELECT
        s.SupplierId,
        s.SupplierToken,
        s.Name,
        s.NormalizedName,
        s.LegalName,
        s.TaxId,
        s.Email,
        s.Phone,
        s.AddressLine1,
        s.AddressLine2,
        s.City,
        s.State,
        s.PostalCode,
        s.Country,
        s.IsGlobal,
        s.SupplierType,
        s.HasAccessToSystem,
        s.IsActive,
        s.IsDeleted,
        owner.OrganizationToken AS OrganizationTokenResult,
        owner.Name              AS OrganizationName,
        s.CreatedUtc,
        s.CreatedBy,
        s.LastUpdatedUtc,
        s.LastUpdatedBy,
        s.DeletedUtc,
        s.DeletedBy,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.Suppliers s
    OUTER APPLY (
        SELECT TOP (1) o.OrganizationToken, o.Name
        FROM dbo.OrganizationSuppliers os
        JOIN dbo.Organizations o ON o.OrganizationId = os.OrganizationId
        WHERE os.SupplierId = s.SupplierId
          AND os.IsActive = 1
        ORDER BY os.OrganizationSupplierId DESC
    ) owner
    WHERE
        s.IsDeleted = 0
        AND (@IncludeInactive = 1 OR s.IsActive = 1)
        AND
        (
            -- SUPERADMIN: unrestricted
            @ContextRoleLevel >= 100

            OR

            -- SUPPLIER USER: see only their own supplier
            (@ContextSupplierId IS NOT NULL AND s.SupplierId = @ContextSupplierId)

            OR

            -- GLOBAL: visible to everyone
            s.IsGlobal = 1

            OR

            -- PRIVATE, owned within caller's own descending hierarchy
            EXISTS (
                SELECT 1
                FROM dbo.OrganizationSuppliers os
                JOIN OrganizationDescendants od ON od.OrganizationId = os.OrganizationId
                WHERE os.SupplierId = s.SupplierId
                  AND os.IsActive = 1
            )
        )
        AND
        (
            @SearchText IS NULL
            OR (@SearchField = 'name'    AND LOWER(s.Name)    LIKE '%' + LOWER(@SearchText) + '%')
            OR (@SearchField = 'email'   AND LOWER(s.Email)   LIKE '%' + LOWER(@SearchText) + '%')
            OR (@SearchField = 'city'    AND LOWER(s.City)    LIKE '%' + LOWER(@SearchText) + '%')
            OR (@SearchField = 'country' AND LOWER(s.Country) LIKE '%' + LOWER(@SearchText) + '%')
            OR (@SearchField = 'taxid'   AND LOWER(s.TaxId)   LIKE '%' + LOWER(@SearchText) + '%')
        )
    ORDER BY s.SupplierId
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
