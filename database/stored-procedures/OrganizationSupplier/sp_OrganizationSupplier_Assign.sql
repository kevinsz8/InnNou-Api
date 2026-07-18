SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORGANIZATIONSUPPLIER - ASSIGN
   Assigns (or reassigns) the owning organization of a private
   supplier. App-level invariant: at most one ACTIVE row per
   SupplierId at a time — deactivates any OTHER organization's
   active ownership row for this SupplierId first, then inserts or
   reactivates the (OrganizationId, SupplierId) row for the target
   owner (upsert-shaped, since a prior ownership cycle — e.g.
   Org A -> Org B -> Org A again — may have left an inactive row
   already sitting on the unique (OrganizationId, SupplierId)
   index). Must be called within the SAME ambient transaction as
   the Suppliers insert/update it accompanies — it does not open
   its own transaction.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_OrganizationSupplier_Assign
(
    @OrganizationId  INT,
    @SupplierId      INT,
    @CreatedUtc      DATETIME2(7) = NULL,
    @CreatedBy       VARCHAR(150) = NULL,
    @LastUpdatedUtc  DATETIME2(7) = NULL,
    @LastUpdatedBy   VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.OrganizationSuppliers
    SET IsActive       = 0,
        LastUpdatedUtc = ISNULL(@LastUpdatedUtc, SYSUTCDATETIME()),
        LastUpdatedBy  = @LastUpdatedBy
    WHERE SupplierId = @SupplierId
      AND OrganizationId <> @OrganizationId
      AND IsActive = 1;

    IF EXISTS (SELECT 1 FROM dbo.OrganizationSuppliers WHERE OrganizationId = @OrganizationId AND SupplierId = @SupplierId)
    BEGIN
        UPDATE dbo.OrganizationSuppliers
        SET IsActive       = 1,
            LastUpdatedUtc = ISNULL(@LastUpdatedUtc, SYSUTCDATETIME()),
            LastUpdatedBy  = @LastUpdatedBy
        WHERE OrganizationId = @OrganizationId AND SupplierId = @SupplierId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.OrganizationSuppliers (OrganizationId, SupplierId, IsActive, CreatedUtc, CreatedBy)
        VALUES (@OrganizationId, @SupplierId, 1, ISNULL(@CreatedUtc, SYSUTCDATETIME()), @CreatedBy);
    END

    SELECT OrganizationSupplierId, OrganizationId, SupplierId, IsActive,
           CreatedUtc, CreatedBy, LastUpdatedUtc, LastUpdatedBy
    FROM dbo.OrganizationSuppliers
    WHERE OrganizationId = @OrganizationId AND SupplierId = @SupplierId;
END;
GO
