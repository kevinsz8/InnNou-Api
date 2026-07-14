SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIER - GET BY NORMALIZED NAME
   Returns a single active, non-deleted supplier by its normalized
   name. Mirrors sp_Organization_GetByNormalizedName's shape; used
   by Article/ArticlePrice bulk import to resolve an Excel
   "SupplierName" column to a SupplierId without requiring the
   caller to know the supplier's token/id. NormalizedName has only
   a non-unique index (UX_Suppliers_NormalizedName_NotDeleted), so
   TOP 1 with a deterministic ORDER BY is required.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Supplier_GetByNormalizedName
(
    @NormalizedName VARCHAR(200)
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
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
        IsDeleted
    FROM dbo.Suppliers
    WHERE NormalizedName = @NormalizedName
      AND IsActive = 1
      AND IsDeleted = 0
    ORDER BY SupplierId;
END;
GO
