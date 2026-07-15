SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIER - SOFT DELETE
   Marks a supplier as deleted and inactive, recording the full
   deleted audit trail. Does not physically remove the row.
   See sp_Supplier_Create's header comment for why the SET
   statements above are required (filtered-index gotcha).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Supplier_SoftDelete
(
    @SupplierToken  UNIQUEIDENTIFIER,
    @IsDeleted      BIT,
    @LastUpdatedUtc DATETIME2(7),
    @LastUpdatedBy  VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Suppliers
    SET
        IsDeleted      = @IsDeleted,
        IsActive       = 0,
        LastUpdatedUtc = @LastUpdatedUtc,
        LastUpdatedBy  = @LastUpdatedBy,
        DeletedUtc     = @LastUpdatedUtc,
        DeletedBy      = @LastUpdatedBy
    WHERE SupplierToken = @SupplierToken;
END;
GO
