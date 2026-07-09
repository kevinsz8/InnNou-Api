CREATE OR ALTER PROCEDURE sp_Article_GetPaged
(
    @PageNumber      INT,
    @PageSize        INT,
    @SupplierId      INT           = NULL,
    @FamilyId        INT           = NULL,
    @SubFamilyId     INT           = NULL,
    @SearchText      VARCHAR(200)  = NULL,
    @IncludeInactive BIT           = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.ArticleId,
        a.ArticleToken,
        a.SupplierId,
        s.Name          AS SupplierName,
        a.Name,
        a.NormalizedName,
        a.Description,
        a.SupplierSku,
        a.Barcode,
        a.Brand,
        a.FamilyId,
        f.Code          AS FamilyCode,
        a.SubFamilyId,
        sf.Code         AS SubFamilyCode,
        a.PurchaseUnitId,
        pu.Code         AS PurchaseUnitCode,
        pu.Symbol       AS PurchaseUnitSymbol,
        a.PurchaseQuantity,
        a.ContentUnitId,
        cu.Code         AS ContentUnitCode,
        cu.Symbol       AS ContentUnitSymbol,
        a.ContentQuantity,
        a.BaseUnitId,
        bu.Code         AS BaseUnitCode,
        bu.Symbol       AS BaseUnitSymbol,
        a.MinimumOrderQty,
        a.LeadTimeDays,
        a.IsActive,
        a.IsDeleted,
        a.ReplacedByArticleId,
        r.ArticleToken  AS ReplacedByArticleToken,
        COUNT(*) OVER() AS TotalCount
    FROM   Articles        a
    JOIN   Suppliers       s  ON  s.SupplierId       = a.SupplierId
    JOIN   UnitsOfMeasure  pu ON  pu.UnitOfMeasureId = a.PurchaseUnitId
    JOIN   UnitsOfMeasure  cu ON  cu.UnitOfMeasureId = a.ContentUnitId
    LEFT JOIN UnitsOfMeasure bu ON bu.UnitOfMeasureId = a.BaseUnitId
    LEFT JOIN Families     f  ON  f.FamilyId         = a.FamilyId
    LEFT JOIN SubFamilies  sf ON  sf.SubFamilyId      = a.SubFamilyId
    LEFT JOIN Articles     r  ON  r.ArticleId         = a.ReplacedByArticleId
    WHERE  a.IsDeleted = 0
      AND  (@IncludeInactive = 1 OR a.IsActive = 1)
      AND  (@SupplierId  IS NULL OR a.SupplierId  = @SupplierId)
      AND  (@FamilyId    IS NULL OR a.FamilyId    = @FamilyId)
      AND  (@SubFamilyId IS NULL OR a.SubFamilyId = @SubFamilyId)
      AND  (@SearchText  IS NULL OR
            a.NormalizedName LIKE '%' + UPPER(@SearchText) + '%' OR
            a.SupplierSku    LIKE '%' + @SearchText + '%' OR
            a.Barcode        LIKE '%' + @SearchText + '%' OR
            a.Brand          LIKE '%' + @SearchText + '%')
    ORDER BY s.Name, a.Name
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
