/* =============================================================
   SUPPLIER - GET BY TOKEN
   Returns a single non-deleted supplier by its token.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Supplier_GetByToken
(
    @SupplierToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        SupplierId,
        SupplierToken,
        Name,
        NormalizedName,
        LegalName,
        TaxId,
        Email,
        Phone,
        AddressLine1,
        AddressLine2,
        City,
        State,
        PostalCode,
        Country,
        IsGlobal,
        HasAccessToSystem,
        IsActive,
        IsDeleted,
        CreatedUtc,
        CreatedBy,
        LastUpdatedUtc,
        LastUpdatedBy,
        DeletedUtc,
        DeletedBy
    FROM dbo.Suppliers
    WHERE SupplierToken = @SupplierToken
      AND IsDeleted = 0;
END;
GO
