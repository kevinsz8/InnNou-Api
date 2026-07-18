SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORGANIZATIONSUPPLIER - GET ACTIVE BY SUPPLIER ID
   Resolves the current owning organization of a private supplier
   (the single ACTIVE row for @SupplierId). Returns no rows for a
   global supplier or one with no ownership row yet.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_OrganizationSupplier_GetActiveBySupplierId
(
    @SupplierId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        os.OrganizationSupplierId, os.OrganizationId, o.OrganizationToken, o.Name AS OrganizationName,
        os.SupplierId, os.IsActive, os.CreatedUtc, os.CreatedBy, os.LastUpdatedUtc, os.LastUpdatedBy
    FROM dbo.OrganizationSuppliers os
    JOIN dbo.Organizations o ON o.OrganizationId = os.OrganizationId
    WHERE os.SupplierId = @SupplierId
      AND os.IsActive = 1
    ORDER BY os.OrganizationSupplierId DESC;
END;
GO
