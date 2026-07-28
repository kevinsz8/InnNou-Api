SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   STOCKLEVEL - GET PAGED
   Same hierarchy-descent CTE shape as sp_PurchaseOrder_GetPaged.
   @WarehouseId/@ArticleId optionally narrow within the resolved scope.

   FamilyCode/SubFamilyCode: simple LEFT JOIN off Articles, same as
   OrderLine's own live-resolved (not frozen) Family/SubFamily.
   CategoryCode/SubCategoryCode: NOT a plain Article column — Category
   ownership is resolved per-Organization via ArticleClassifications,
   same ascending-ancestry-walk logic sp_ArticleClassification_
   GetEffectiveForArticle uses for a single article. Inlined here as a
   set-based CTE (EffectiveArticleClassification) rather than called
   per-row, since a paged multi-row result can span more than one
   Organization within @RootOrganizationId's hierarchy.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_StockLevel_GetPaged
(
    @RootOrganizationId INT = NULL,
    @WarehouseId        INT = NULL,
    @ArticleId          INT = NULL,
    @SearchText         VARCHAR(200) = NULL,
    @FamilyId           INT = NULL,
    @SubFamilyId        INT = NULL,
    @CategoryId         INT = NULL,
    @SubCategoryId      INT = NULL,
    @PageNumber         INT,
    @PageSize           INT
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
    ),
    OrgAncestry AS
    (
        SELECT o.OrganizationId AS StartOrgId, o.OrganizationId, o.ParentOrganizationId, 0 AS Depth
        FROM dbo.Organizations o
        WHERE o.IsDeleted = 0 AND o.IsActive = 1

        UNION ALL

        SELECT oa.StartOrgId, o.OrganizationId, o.ParentOrganizationId, oa.Depth + 1
        FROM dbo.Organizations o
        INNER JOIN OrgAncestry oa ON o.OrganizationId = oa.ParentOrganizationId
        WHERE o.IsDeleted = 0 AND o.IsActive = 1
    ),
    EffectiveArticleClassification AS
    (
        SELECT oa.StartOrgId AS OrganizationId, ac.ArticleId, ac.CategoryId, ac.SubCategoryId,
               ROW_NUMBER() OVER (PARTITION BY oa.StartOrgId, ac.ArticleId ORDER BY oa.Depth ASC) AS rn
        FROM OrgAncestry oa
        INNER JOIN dbo.ArticleClassifications ac ON ac.OrganizationId = oa.OrganizationId
    )
    SELECT
        sl.StockLevelId, sl.StockLevelToken,
        sl.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName, w.OrganizationId,
        sl.ArticleId, a.ArticleToken, a.Name AS ArticleName, a.SupplierId, s.Name AS SupplierName,
        a.PurchaseUnitId, u.Code AS PurchaseUnitCode,
        f.Code AS FamilyCode, sf.Code AS SubFamilyCode,
        cat.Code AS CategoryCode, subcat.Code AS SubCategoryCode,
        sl.QuantityOnHand,
        sl.CreatedUtc, sl.CreatedBy, sl.LastUpdatedUtc, sl.LastUpdatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.StockLevels sl
    JOIN dbo.Warehouses w      ON w.WarehouseId      = sl.WarehouseId
    JOIN dbo.Articles a        ON a.ArticleId        = sl.ArticleId
    JOIN dbo.Suppliers s       ON s.SupplierId       = a.SupplierId
    JOIN dbo.UnitsOfMeasure u  ON u.UnitOfMeasureId  = a.PurchaseUnitId
    LEFT JOIN dbo.Families f      ON f.FamilyId      = a.FamilyId
    LEFT JOIN dbo.SubFamilies sf  ON sf.SubFamilyId  = a.SubFamilyId
    LEFT JOIN EffectiveArticleClassification eac ON eac.OrganizationId = w.OrganizationId AND eac.ArticleId = a.ArticleId AND eac.rn = 1
    LEFT JOIN dbo.Categories cat       ON cat.CategoryId = eac.CategoryId
    LEFT JOIN dbo.SubCategories subcat ON subcat.SubCategoryId = eac.SubCategoryId
    WHERE (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = w.OrganizationId))
      AND (@WarehouseId IS NULL OR sl.WarehouseId = @WarehouseId)
      AND (@ArticleId IS NULL OR sl.ArticleId = @ArticleId)
      AND (@SearchText IS NULL OR a.NormalizedName LIKE '%' + UPPER(@SearchText) + '%')
      AND (@FamilyId IS NULL OR a.FamilyId = @FamilyId)
      AND (@SubFamilyId IS NULL OR a.SubFamilyId = @SubFamilyId)
      AND (@CategoryId IS NULL OR eac.CategoryId = @CategoryId)
      AND (@SubCategoryId IS NULL OR eac.SubCategoryId = @SubCategoryId)
    ORDER BY a.Name
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
