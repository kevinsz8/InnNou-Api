/* =============================================================
   SUPPLIER - GET BY TOKEN
   Returns a single non-deleted supplier by its token, denormalizing
   its current owning organization (if private) via OUTER APPLY —
   NULL/NULL for a global supplier. No visibility filtering here —
   this is a plain lookup by an already-known token; the calling
   service applies whatever scope check fits the caller (see
   SupplierService.GetSupplierByTokenAsync/EditSupplierAsync).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Supplier_GetByToken
(
    @SupplierToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        s.SupplierId,
        s.SupplierToken,
        s.Name,
        s.NormalizedName,
        s.LegalName,
        s.TaxId,
        s.Email,
        s.Phone,
        s.AddressLine1,
        s.AddressLine2,
        s.City,
        s.State,
        s.PostalCode,
        s.Country,
        s.IsGlobal,
        st.Code AS SupplierType,
        s.LogoUrl,
        s.HasAccessToSystem,
        s.IsActive,
        s.IsDeleted,
        owner.OrganizationToken AS OrganizationTokenResult,
        owner.Name              AS OrganizationName,
        s.CreatedUtc,
        s.CreatedBy,
        s.LastUpdatedUtc,
        s.LastUpdatedBy,
        s.DeletedUtc,
        s.DeletedBy
    FROM dbo.Suppliers s
    JOIN dbo.SupplierTypes st ON st.SupplierTypeId = s.SupplierTypeId
    OUTER APPLY (
        SELECT TOP (1) o.OrganizationToken, o.Name
        FROM dbo.OrganizationSuppliers os
        JOIN dbo.Organizations o ON o.OrganizationId = os.OrganizationId
        WHERE os.SupplierId = s.SupplierId
          AND os.IsActive = 1
        ORDER BY os.OrganizationSupplierId DESC
    ) owner
    WHERE s.SupplierToken = @SupplierToken
      AND s.IsDeleted = 0;
END;
GO
