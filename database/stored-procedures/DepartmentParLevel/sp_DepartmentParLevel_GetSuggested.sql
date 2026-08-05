SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   DEPARTMENTPARLEVEL - GET SUGGESTED
   Paged list of (Department, Article) pairs that are "due" for a new
   Requisition, driven FROM DepartmentParLevels (a pair only appears here if
   a base row has been configured for it) -- same "driven from the config
   table" shape as sp_ParLevel_GetBelowPar.

   Critical difference from Warehouse Par Levels: a Department has no
   StockLevels of its own, so "below minimum" can't be a live balance
   comparison. Instead this resolves a consumption-pace + elapsed-time
   signal from real CONSUMPTION history (InventoryMovements ->
   RequisitionIssueLines -> RequisitionLines -> Requisitions), never a
   fabricated on-hand number:

     AvgDailyConsumption = (sum of CONSUMPTION quantity in the last 90 days) / 90
     ExpectedCycleDays    = MinimumQuantity / AvgDailyConsumption
                            ("how many days would MinimumQuantity worth of
                            stock last at the department's own recent pace")
     Suggested when        DaysSinceLastIssued >= ExpectedCycleDays

   A pair with zero CONSUMPTION in the lookback window never appears here
   (the INNER JOIN against Consumption excludes it) -- there's no honest
   rate to project from yet, so no suggestion is fabricated. See
   .claude/RequisitionsModule.md for the full reasoning and worked example.

   SuggestedQuantity is always the configured ReorderQuantity (a static
   value the human set), never a derived/computed amount -- same
   "suggest, don't auto-execute or auto-calculate the ask" philosophy as
   Warehouse Par Levels' own ReorderQuantity.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_DepartmentParLevel_GetSuggested
(
    @RootOrganizationId INT = NULL,
    @DepartmentId       INT = NULL,
    @ArticleId          INT = NULL,
    @PageNumber         INT,
    @PageSize           INT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    DECLARE @LookbackStart DATETIME2 = DATEADD(DAY, -90, @Now);

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
    Consumption AS
    (
        SELECT
            r.DepartmentId,
            rl.ArticleId,
            SUM(-im.Quantity) AS TotalConsumed90d,
            MAX(ri.CreatedUtc) AS LastIssuedUtc
        FROM dbo.InventoryMovements im
        JOIN dbo.RequisitionIssueLines ril ON ril.RequisitionIssueLineId = im.RequisitionIssueLineId
        JOIN dbo.RequisitionIssues ri      ON ri.RequisitionIssueId      = ril.RequisitionIssueId
        JOIN dbo.RequisitionLines rl       ON rl.RequisitionLineId       = ril.RequisitionLineId
        JOIN dbo.Requisitions r            ON r.RequisitionId            = rl.RequisitionId
        WHERE im.InventoryMovementTypeId = 7 -- CONSUMPTION
          AND im.CreatedUtc >= @LookbackStart
        GROUP BY r.DepartmentId, rl.ArticleId
    )
    SELECT
        dpl.DepartmentParLevelId, dpl.DepartmentParLevelToken,
        dpl.DepartmentId, d.DepartmentToken, d.Name AS DepartmentName, d.OrganizationId,
        dpl.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        a.PurchaseUnitId, u.Code AS PurchaseUnitCode,
        dpl.MinimumQuantity, dpl.ReorderQuantity AS SuggestedQuantity,
        c.TotalConsumed90d,
        CAST(c.TotalConsumed90d / 90.0 AS DECIMAL(18,8)) AS AvgDailyConsumption,
        c.LastIssuedUtc,
        DATEDIFF(DAY, c.LastIssuedUtc, @Now) AS DaysSinceLastIssued,
        CAST(dpl.MinimumQuantity / (c.TotalConsumed90d / 90.0) AS DECIMAL(18,4)) AS ExpectedCycleDays,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.DepartmentParLevels dpl
    JOIN dbo.Departments d    ON d.DepartmentId = dpl.DepartmentId
    JOIN dbo.Articles a       ON a.ArticleId    = dpl.ArticleId
    JOIN dbo.UnitsOfMeasure u ON u.UnitOfMeasureId = a.PurchaseUnitId
    JOIN Consumption c ON c.DepartmentId = dpl.DepartmentId AND c.ArticleId = dpl.ArticleId
    WHERE dpl.IsActive = 1
      AND d.IsActive = 1
      AND c.TotalConsumed90d > 0
      AND DATEDIFF(DAY, c.LastIssuedUtc, @Now) >= (dpl.MinimumQuantity / (c.TotalConsumed90d / 90.0))
      AND (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = d.OrganizationId))
      AND (@DepartmentId IS NULL OR dpl.DepartmentId = @DepartmentId)
      AND (@ArticleId IS NULL OR dpl.ArticleId = @ArticleId)
    ORDER BY (DATEDIFF(DAY, c.LastIssuedUtc, @Now) - (dpl.MinimumQuantity / (c.TotalConsumed90d / 90.0))) DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
