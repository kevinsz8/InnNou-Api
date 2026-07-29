SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INVENTORYTRANSFER - GET PAGED
   Same hierarchy-descent CTE shape as sp_PurchaseOrder_GetPaged, scoped by
   the FromWarehouse's Organization (both warehouses always share the same
   Organization — enforced at create time, see InventoryService.CreateTransferAsync).
   @WarehouseId optionally narrows to transfers where it's either side.

   @FromDate/@ToDate filter on CreatedUtc (inclusive both ends — @ToDate is
   bumped a full day so a caller passing a bare date still captures every
   transfer created that day regardless of time-of-day).

   @ArticleId/@FamilyId/@SubFamilyId/@CategoryId/@SubCategoryId all filter
   at the HEADER level via an EXISTS over InventoryTransferLines — a
   transfer can carry many articles, so "match this article/classification"
   means "at least one line matches", not every line. CategoryId/
   SubCategoryId resolution reuses the same set-based EffectiveArticleClassification
   CTE sp_StockLevel_GetPaged/sp_ParLevel_GetBelowPar already established
   (ascending ancestor walk, nearest-organization-wins via ROW_NUMBER). The
   EXISTS is short-circuited when none of the five filters are set, so a
   plain date/warehouse-only search never pays for the extra join.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InventoryTransfer_GetPaged
(
    @RootOrganizationId INT = NULL,
    @WarehouseId        INT = NULL,
    @FromDate           DATE = NULL,
    @ToDate             DATE = NULL,
    @ArticleId          INT = NULL,
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

    DECLARE @ToDateExclusive DATETIME2 = CASE WHEN @ToDate IS NULL THEN NULL ELSE DATEADD(DAY, 1, CAST(@ToDate AS DATETIME2)) END;

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
        it.InventoryTransferId, it.InventoryTransferToken,
        it.FromWarehouseId, fw.WarehouseToken AS FromWarehouseToken, fw.Name AS FromWarehouseName,
        it.ToWarehouseId, tw.WarehouseToken AS ToWarehouseToken, tw.Name AS ToWarehouseName,
        it.Notes, it.CreatedUtc, it.CreatedBy,
        lc.LineCount,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.InventoryTransfers it
    JOIN dbo.Warehouses fw ON fw.WarehouseId = it.FromWarehouseId
    JOIN dbo.Warehouses tw ON tw.WarehouseId = it.ToWarehouseId
    CROSS APPLY (SELECT COUNT(*) AS LineCount FROM dbo.InventoryTransferLines l WHERE l.InventoryTransferId = it.InventoryTransferId) lc
    WHERE (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = fw.OrganizationId))
      AND (@WarehouseId IS NULL OR it.FromWarehouseId = @WarehouseId OR it.ToWarehouseId = @WarehouseId)
      AND (@FromDate IS NULL OR it.CreatedUtc >= @FromDate)
      AND (@ToDateExclusive IS NULL OR it.CreatedUtc < @ToDateExclusive)
      AND (
            (@ArticleId IS NULL AND @FamilyId IS NULL AND @SubFamilyId IS NULL AND @CategoryId IS NULL AND @SubCategoryId IS NULL)
            OR EXISTS (
                SELECT 1
                FROM dbo.InventoryTransferLines l
                JOIN dbo.Articles a ON a.ArticleId = l.ArticleId
                LEFT JOIN EffectiveArticleClassification eac ON eac.OrganizationId = fw.OrganizationId AND eac.ArticleId = a.ArticleId AND eac.rn = 1
                WHERE l.InventoryTransferId = it.InventoryTransferId
                  AND (@ArticleId IS NULL OR a.ArticleId = @ArticleId)
                  AND (@FamilyId IS NULL OR a.FamilyId = @FamilyId)
                  AND (@SubFamilyId IS NULL OR a.SubFamilyId = @SubFamilyId)
                  AND (@CategoryId IS NULL OR eac.CategoryId = @CategoryId)
                  AND (@SubCategoryId IS NULL OR eac.SubCategoryId = @SubCategoryId)
            )
          )
    ORDER BY it.CreatedUtc DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
