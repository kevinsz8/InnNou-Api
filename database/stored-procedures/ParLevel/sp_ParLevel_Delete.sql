SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PARLEVEL - DELETE
   Hard delete — operational configuration, not an audit/financial record,
   same reasoning as OrderTemplate's own hard-delete shape. Deletes any
   ParLevelOverrides for the same (Warehouse, Article) first — an override
   is meaningless without the base row it refines (ParLevelService requires
   a base to exist before an override can even be created).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_ParLevel_Delete
(
    @ParLevelToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @WarehouseId INT, @ArticleId INT;

    SELECT @WarehouseId = WarehouseId, @ArticleId = ArticleId
    FROM dbo.ParLevels
    WHERE ParLevelToken = @ParLevelToken;

    IF @WarehouseId IS NOT NULL
    BEGIN
        DELETE FROM dbo.ParLevelOverrides WHERE WarehouseId = @WarehouseId AND ArticleId = @ArticleId;
        DELETE FROM dbo.ParLevels WHERE ParLevelToken = @ParLevelToken;
    END

    SELECT CAST(CASE WHEN @WarehouseId IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS Deleted;
END;
GO
