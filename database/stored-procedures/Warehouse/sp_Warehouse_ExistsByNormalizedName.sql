SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   WAREHOUSE - EXISTS BY NORMALIZED NAME
   Scoped per-organization (two organizations may each have a
   warehouse named "General Warehouse").
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Warehouse_ExistsByNormalizedName
(
    @OrganizationId  INT,
    @NormalizedName  VARCHAR(200),
    @ExcludeWarehouseToken UNIQUEIDENTIFIER = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CASE WHEN EXISTS
    (
        SELECT 1
        FROM dbo.Warehouses
        WHERE OrganizationId = @OrganizationId
          AND NormalizedName = @NormalizedName
          AND IsDeleted = 0
          AND (@ExcludeWarehouseToken IS NULL OR WarehouseToken <> @ExcludeWarehouseToken)
    )
    THEN 1 ELSE 0 END;
END;
GO
