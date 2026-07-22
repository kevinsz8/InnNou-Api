SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIER - SET LOGO URL
   Sets or clears (NULL) the supplier's logo path — the actual image file
   lives on local disk (see CLAUDE.md's "Supplier logo" note), this only
   persists the relative URL used to fetch it. Only acts on non-deleted
   records.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Supplier_SetLogoUrl
(
    @SupplierToken  UNIQUEIDENTIFIER,
    @LogoUrl        NVARCHAR(500) = NULL,
    @LastUpdatedUtc DATETIME2(7),
    @LastUpdatedBy  VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Suppliers
    SET
        LogoUrl        = @LogoUrl,
        LastUpdatedUtc = @LastUpdatedUtc,
        LastUpdatedBy  = @LastUpdatedBy
    WHERE SupplierToken = @SupplierToken
      AND IsDeleted = 0;

    SELECT
        s.SupplierId, s.SupplierToken, s.Name, s.NormalizedName, s.LegalName, s.TaxId,
        s.Email, s.Phone, s.AddressLine1, s.AddressLine2, s.City, s.State,
        s.PostalCode, s.Country, s.IsGlobal, st.Code AS SupplierType, s.LogoUrl, s.HasAccessToSystem, s.IsActive, s.IsDeleted,
        s.CreatedUtc, s.CreatedBy, s.LastUpdatedUtc, s.LastUpdatedBy, s.DeletedUtc, s.DeletedBy
    FROM dbo.Suppliers s
    JOIN dbo.SupplierTypes st ON st.SupplierTypeId = s.SupplierTypeId
    WHERE s.SupplierToken = @SupplierToken;
END;
GO
