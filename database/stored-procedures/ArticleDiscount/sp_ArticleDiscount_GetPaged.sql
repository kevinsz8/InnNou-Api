-- Always scoped to a single Supplier — discounts have no "global" browse view, same convention
-- as FamilyApprovalThreshold's required @OrganizationId.
CREATE OR ALTER PROCEDURE sp_ArticleDiscount_GetPaged
(
    @SupplierId      INT,
    @PageNumber      INT,
    @PageSize        INT,
    @IncludeInactive BIT = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.ArticleDiscountId, d.ArticleDiscountToken,
        d.SupplierId, s.SupplierToken, s.Name AS SupplierName,
        d.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        d.SubFamilyId, sf.SubFamilyToken, sf.Code AS SubFamilyCode,
        d.FamilyId, f.FamilyToken, f.Code AS FamilyCode,
        d.DiscountTypeId, dt.Code AS DiscountTypeCode, d.DiscountValue, d.CurrencyCode,
        d.EffectiveFrom, d.EffectiveUntil, d.Description,
        d.IsActive, d.CreatedUtc, d.CreatedBy, d.LastUpdatedUtc, d.LastUpdatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM ArticleDiscounts d
    JOIN Suppliers s              ON s.SupplierId    = d.SupplierId
    JOIN DiscountTypes dt         ON dt.DiscountTypeId = d.DiscountTypeId
    LEFT JOIN Articles a          ON a.ArticleId     = d.ArticleId
    LEFT JOIN SubFamilies sf      ON sf.SubFamilyId  = d.SubFamilyId
    LEFT JOIN Families f          ON f.FamilyId      = d.FamilyId
    WHERE d.SupplierId = @SupplierId
      AND (@IncludeInactive = 1 OR d.IsActive = 1)
    ORDER BY d.EffectiveFrom DESC, d.CreatedUtc DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
