/* =============================================================
   SUPPLIER - SOFT DELETE
   Marks a supplier as deleted and inactive, recording the full
   deleted audit trail. Does not physically remove the row.
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
