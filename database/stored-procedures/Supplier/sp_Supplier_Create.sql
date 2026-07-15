SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIER - CREATE
   Inserts a new supplier and returns the full created row.
   Gotcha (see InnNou-Api CLAUDE.md, "Article pricing"): the SET
   ANSI_NULLS/QUOTED_IDENTIFIER above are required here because
   Suppliers has a filtered index — SQL Server captures those
   session options at CREATE PROCEDURE compile time, not from the
   caller's session at execution time. Without them, INSERTs
   against this table fail with error 1934 the moment this
   procedure is ever redeployed from a session with different
   ambient settings — this file was missing them until this fix.
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
    @IsGlobal           BIT,
    @SupplierType       VARCHAR(20),
    @HasAccessToSystem  BIT,
    @IsActive           BIT,
    @IsDeleted          BIT,
    @CreatedUtc         DATETIME2(7),
    @CreatedBy          VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Suppliers
    (
        SupplierToken, Name, NormalizedName, LegalName, TaxId,
        Email, Phone, AddressLine1, AddressLine2, City, State,
        PostalCode, Country, IsGlobal, SupplierType, HasAccessToSystem, IsActive, IsDeleted,
        CreatedUtc, CreatedBy
    )
    VALUES
    (
        @SupplierToken, @Name, @NormalizedName, @LegalName, @TaxId,
        @Email, @Phone, @AddressLine1, @AddressLine2, @City, @State,
        @PostalCode, @Country, @IsGlobal, @SupplierType, @HasAccessToSystem, @IsActive, @IsDeleted,
        @CreatedUtc, @CreatedBy
    );

    SELECT
        SupplierId, SupplierToken, Name, NormalizedName, LegalName, TaxId,
        Email, Phone, AddressLine1, AddressLine2, City, State,
        PostalCode, Country, IsGlobal, SupplierType, HasAccessToSystem, IsActive, IsDeleted,
        CreatedUtc, CreatedBy, LastUpdatedUtc, LastUpdatedBy, DeletedUtc, DeletedBy
    FROM dbo.Suppliers
    WHERE SupplierToken = @SupplierToken;
END;
GO
