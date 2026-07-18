SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORGANIZATIONSUPPLIER - DEACTIVATE ALL
   Deactivates every active ownership row for a supplier — used
   when a private supplier becomes Global (there is no new owner
   to assign, only the old one to clear). Kept as a distinct,
   narrower SP from sp_OrganizationSupplier_Assign rather than
   overloading that one with a nullable target organization.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_OrganizationSupplier_DeactivateAll
(
    @SupplierId     INT,
    @LastUpdatedUtc DATETIME2(7) = NULL,
    @LastUpdatedBy  VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.OrganizationSuppliers
    SET IsActive       = 0,
        LastUpdatedUtc = ISNULL(@LastUpdatedUtc, SYSUTCDATETIME()),
        LastUpdatedBy  = @LastUpdatedBy
    WHERE SupplierId = @SupplierId
      AND IsActive = 1;
END;
GO
