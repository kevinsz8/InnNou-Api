SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   WAREHOUSE - SOFT DELETE
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Warehouse_SoftDelete
(
    @WarehouseToken UNIQUEIDENTIFIER,
    @DeletedUtc     DATETIME2,
    @DeletedBy      VARCHAR(150) = NULL,
    @LastUpdatedUtc DATETIME2,
    @LastUpdatedBy  VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Warehouses
    SET
        IsActive       = 0,
        IsDeleted      = 1,
        DeletedUtc     = @DeletedUtc,
        DeletedBy      = @DeletedBy,
        LastUpdatedUtc = @LastUpdatedUtc,
        LastUpdatedBy  = @LastUpdatedBy
    WHERE WarehouseToken = @WarehouseToken
      AND IsDeleted = 0;
END;
GO
