SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PARLEVELOVERRIDE - CREATE
   Plain insert + re-select — overlap validation and calendar-validity
   checks (Feb 29 rejected, StartDate<=EndDate, etc.) happen in C#
   (ParLevelService.CreateOverrideAsync) before this is called, same
   division of labor as ArticlePrice/ConsolidatedPurchaseOrder's own
   date-based validations. @Type resolved to Id via the same inline-
   subquery pattern as sp_InventoryMovement_Create's @Type param.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_ParLevelOverride_Create
(
    @ParLevelOverrideToken UNIQUEIDENTIFIER,
    @WarehouseId           INT,
    @ArticleId             INT,
    @Type                  VARCHAR(20),
    @Label                 NVARCHAR(200)  = NULL,
    @MinimumQuantity       DECIMAL(18,4),
    @ReorderQuantity       DECIMAL(18,4),
    @StartMonth            TINYINT        = NULL,
    @StartDay              TINYINT        = NULL,
    @EndMonth              TINYINT        = NULL,
    @EndDay                TINYINT        = NULL,
    @StartDate             DATE           = NULL,
    @EndDate               DATE           = NULL,
    @CreatedBy             VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.ParLevelOverrides
        (ParLevelOverrideToken, WarehouseId, ArticleId, ParLevelOverrideTypeId, Label,
         MinimumQuantity, ReorderQuantity, StartMonth, StartDay, EndMonth, EndDay, StartDate, EndDate, CreatedBy)
    VALUES
        (@ParLevelOverrideToken, @WarehouseId, @ArticleId,
         (SELECT ParLevelOverrideTypeId FROM dbo.ParLevelOverrideTypes WHERE Code = @Type),
         @Label, @MinimumQuantity, @ReorderQuantity, @StartMonth, @StartDay, @EndMonth, @EndDay, @StartDate, @EndDate, @CreatedBy);

    SELECT
        o.ParLevelOverrideId, o.ParLevelOverrideToken,
        o.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName, w.OrganizationId,
        o.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        t.Code AS Type, o.Label,
        o.MinimumQuantity, o.ReorderQuantity,
        o.StartMonth, o.StartDay, o.EndMonth, o.EndDay,
        o.StartDate, o.EndDate,
        o.CreatedUtc, o.CreatedBy
    FROM dbo.ParLevelOverrides o
    JOIN dbo.Warehouses w ON w.WarehouseId = o.WarehouseId
    JOIN dbo.Articles a   ON a.ArticleId   = o.ArticleId
    JOIN dbo.ParLevelOverrideTypes t ON t.ParLevelOverrideTypeId = o.ParLevelOverrideTypeId
    WHERE o.ParLevelOverrideToken = @ParLevelOverrideToken;
END;
GO
