/* =============================================================
   SUPPLIER - GET PAGED
   Returns a paginated list of suppliers. Super admins see all.
   Supplier-scoped users see only their own supplier.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Supplier_GetPaged
(
    @ContextRoleLevel  INT,
    @ContextSupplierId INT          = NULL,
    @SearchField       VARCHAR(50)  = NULL,
    @SearchText        VARCHAR(200) = NULL,
    @PageNumber        INT,
    @PageSize          INT,
    @IncludeInactive   BIT          = 0
)
AS
BEGIN
    SET NOCOUNT ON;

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
        s.IsActive,
        s.IsDeleted,
        s.CreatedUtc,
        s.CreatedBy,
        s.LastUpdatedUtc,
        s.LastUpdatedBy,
        s.DeletedUtc,
        s.DeletedBy,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.Suppliers s
    WHERE
        s.IsDeleted = 0
        AND (@IncludeInactive = 1 OR s.IsActive = 1)
        AND
        (
            -- SUPER ADMIN: see everything
            @ContextRoleLevel >= 100

            OR

            -- SUPPLIER USER: see only their own supplier
            (
                @ContextSupplierId IS NOT NULL
                AND s.SupplierId = @ContextSupplierId
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
