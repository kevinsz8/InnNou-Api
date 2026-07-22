SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIER - UPDATE
   Updates an existing supplier's fields and returns the full
   updated row. Only acts on non-deleted records.
   See sp_Supplier_Create's header comment for why the SET
   statements above are required (filtered-index gotcha).
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
    @IsGlobal           BIT,
    @SupplierTypeId     INT,
    @HasAccessToSystem  BIT,
    @LastUpdatedUtc     DATETIME2(7),
    @LastUpdatedBy      VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Suppliers
    SET
        Name               = @Name,
        NormalizedName     = @NormalizedName,
        LegalName          = @LegalName,
        TaxId              = @TaxId,
        Email              = @Email,
        Phone              = @Phone,
        AddressLine1       = @AddressLine1,
        AddressLine2       = @AddressLine2,
        City               = @City,
        State              = @State,
        PostalCode         = @PostalCode,
        Country            = @Country,
        IsGlobal           = @IsGlobal,
        SupplierTypeId     = @SupplierTypeId,
        HasAccessToSystem  = @HasAccessToSystem,
        LastUpdatedUtc     = @LastUpdatedUtc,
        LastUpdatedBy      = @LastUpdatedBy
    WHERE SupplierToken = @SupplierToken
      AND IsDeleted = 0;

    SELECT
        s.SupplierId, s.SupplierToken, s.Name, s.NormalizedName, s.LegalName, s.TaxId,
        s.Email, s.Phone, s.AddressLine1, s.AddressLine2, s.City, s.State,
        s.PostalCode, s.Country, s.IsGlobal, st.Code AS SupplierType, s.HasAccessToSystem, s.IsActive, s.IsDeleted,
        s.CreatedUtc, s.CreatedBy, s.LastUpdatedUtc, s.LastUpdatedBy, s.DeletedUtc, s.DeletedBy
    FROM dbo.Suppliers s
    JOIN dbo.SupplierTypes st ON st.SupplierTypeId = s.SupplierTypeId
    WHERE s.SupplierToken = @SupplierToken;
END;
GO
