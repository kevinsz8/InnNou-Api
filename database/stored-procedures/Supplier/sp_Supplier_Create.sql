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
    @LanguageCode   VARCHAR(10)  = NULL,
    @IsGlobal           BIT,
    @SupplierTypeId     INT,
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
        PostalCode, Country, LanguageCode, IsGlobal, SupplierTypeId, HasAccessToSystem, IsActive, IsDeleted,
        CreatedUtc, CreatedBy
    )
    VALUES
    (
        @SupplierToken, @Name, @NormalizedName, @LegalName, @TaxId,
        @Email, @Phone, @AddressLine1, @AddressLine2, @City, @State,
        @PostalCode, @Country, @LanguageCode, @IsGlobal, @SupplierTypeId, @HasAccessToSystem, @IsActive, @IsDeleted,
        @CreatedUtc, @CreatedBy
    );

    SELECT
        s.SupplierId, s.SupplierToken, s.Name, s.NormalizedName, s.LegalName, s.TaxId,
        s.Email, s.Phone, s.AddressLine1, s.AddressLine2, s.City, s.State,
        s.PostalCode, s.Country, s.LanguageCode, s.IsGlobal, st.Code AS SupplierType, s.LogoUrl, s.HasAccessToSystem, s.IsActive, s.IsDeleted,
        s.CreatedUtc, s.CreatedBy, s.LastUpdatedUtc, s.LastUpdatedBy, s.DeletedUtc, s.DeletedBy
    FROM dbo.Suppliers s
    JOIN dbo.SupplierTypes st ON st.SupplierTypeId = s.SupplierTypeId
    WHERE s.SupplierToken = @SupplierToken;
END;
GO
