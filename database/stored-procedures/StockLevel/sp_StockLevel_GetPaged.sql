SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   STOCKLEVEL - GET PAGED
   Same hierarchy-descent CTE shape as sp_PurchaseOrder_GetPaged.
   @WarehouseId/@ArticleId optionally narrow within the resolved scope.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_StockLevel_GetPaged
(
    @RootOrganizationId INT = NULL,
    @WarehouseId        INT = NULL,
    @ArticleId          INT = NULL,
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
    )
    SELECT
        sl.StockLevelId, sl.StockLevelToken,
        sl.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName, w.OrganizationId,
        sl.ArticleId, a.ArticleToken, a.Name AS ArticleName, a.SupplierId, s.Name AS SupplierName,
        a.PurchaseUnitId, u.Code AS PurchaseUnitCode,
        sl.QuantityOnHand,
        sl.CreatedUtc, sl.CreatedBy, sl.LastUpdatedUtc, sl.LastUpdatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.StockLevels sl
    JOIN dbo.Warehouses w      ON w.WarehouseId      = sl.WarehouseId
    JOIN dbo.Articles a        ON a.ArticleId        = sl.ArticleId
    JOIN dbo.Suppliers s       ON s.SupplierId       = a.SupplierId
    JOIN dbo.UnitsOfMeasure u  ON u.UnitOfMeasureId  = a.PurchaseUnitId
    WHERE (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = w.OrganizationId))
      AND (@WarehouseId IS NULL OR sl.WarehouseId = @WarehouseId)
      AND (@ArticleId IS NULL OR sl.ArticleId = @ArticleId)
    ORDER BY a.Name
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
