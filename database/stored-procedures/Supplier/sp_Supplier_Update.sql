/* =============================================================
   SUPPLIER - UPDATE
   Updates an existing supplier's fields and returns the full
   updated row. Only acts on non-deleted records.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Supplier_Update
(
    @SupplierToken  UNIQUEIDENTIFIER,
    @Name           VARCHAR(200),
    @NormalizedName VARCHAR(200),
    @LegalName      VARCHAR(250) = NULL,
    @TaxId          VARCHAR(50)  = NULL,
    @Email          VARCHAR(320) = NULL,
    @Phone          VARCHAR(50)  = NULL,
    @AddressLine1   VARCHAR(250) = NULL,
    @AddressLine2   VARCHAR(250) = NULL,
    @City           VARCHAR(150) = NULL,
    @State          VARCHAR(150) = NULL,
    @PostalCode     VARCHAR(50)  = NULL,
    @Country        VARCHAR(100) = NULL,
    @IsGlobal       BIT,
    @LastUpdatedUtc DATETIME2(7),
    @LastUpdatedBy  VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Suppliers
    SET
        Name           = @Name,
        NormalizedName = @NormalizedName,
        LegalName      = @LegalName,
        TaxId          = @TaxId,
        Email          = @Email,
        Phone          = @Phone,
        AddressLine1   = @AddressLine1,
        AddressLine2   = @AddressLine2,
        City           = @City,
        State          = @State,
        PostalCode     = @PostalCode,
        Country        = @Country,
        IsGlobal       = @IsGlobal,
        LastUpdatedUtc = @LastUpdatedUtc,
        LastUpdatedBy  = @LastUpdatedBy
    WHERE SupplierToken = @SupplierToken
      AND IsDeleted = 0;

    SELECT
        SupplierId, SupplierToken, Name, NormalizedName, LegalName, TaxId,
        Email, Phone, AddressLine1, AddressLine2, City, State,
        PostalCode, Country, IsGlobal, IsActive, IsDeleted,
        CreatedUtc, CreatedBy, LastUpdatedUtc, LastUpdatedBy, DeletedUtc, DeletedBy
    FROM dbo.Suppliers
    WHERE SupplierToken = @SupplierToken;
END;
GO
