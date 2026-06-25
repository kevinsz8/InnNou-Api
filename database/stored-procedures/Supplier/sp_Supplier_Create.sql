/* =============================================================
   SUPPLIER - CREATE
   Inserts a new supplier and returns the full created row.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Supplier_Create
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
    @IsActive       BIT,
    @IsDeleted      BIT,
    @CreatedUtc     DATETIME2(7),
    @CreatedBy      VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Suppliers
    (
        SupplierToken, Name, NormalizedName, LegalName, TaxId,
        Email, Phone, AddressLine1, AddressLine2, City, State,
        PostalCode, Country, IsGlobal, IsActive, IsDeleted,
        CreatedUtc, CreatedBy
    )
    VALUES
    (
        @SupplierToken, @Name, @NormalizedName, @LegalName, @TaxId,
        @Email, @Phone, @AddressLine1, @AddressLine2, @City, @State,
        @PostalCode, @Country, @IsGlobal, @IsActive, @IsDeleted,
        @CreatedUtc, @CreatedBy
    );

    SELECT
        SupplierId, SupplierToken, Name, NormalizedName, LegalName, TaxId,
        Email, Phone, AddressLine1, AddressLine2, City, State,
        PostalCode, Country, IsGlobal, IsActive, IsDeleted,
        CreatedUtc, CreatedBy, LastUpdatedUtc, LastUpdatedBy, DeletedUtc, DeletedBy
    FROM dbo.Suppliers
    WHERE SupplierToken = @SupplierToken;
END;
GO
