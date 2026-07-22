/* =============================================================
   SUPPLIERDELIVERYZONE - CHECK COVERAGE
   Single-round-trip gate used by OrderService.AddLineAsync. Resolves
   @WarehouseId's own Zone (never the Organization's, and never the
   acting/impersonating user's — the Warehouse is what actually
   receives the delivery, a single Organization can have warehouses in
   different zones) and reports whether @SupplierId has at least one
   coverage row for it. EnforcementActive is false (never blocks) when
   the warehouse has no ZoneId set yet, or its owning organization
   isn't an ASSOCIATE-type org (Super Asociado orgs are never
   zoned/filtered) — day-of-week is deliberately not considered here,
   coverage on ANY day is enough.
   ============================================================= */
CREATE OR ALTER PROCEDURE sp_SupplierDeliveryZone_CheckCoverage
    @SupplierId  INT,
    @WarehouseId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        w.ZoneId AS WarehouseZoneId,
        CASE WHEN w.ZoneId IS NOT NULL AND ot.Code = 'ASSOCIATE'
             THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS EnforcementActive,
        CASE WHEN EXISTS (
            SELECT 1 FROM SupplierDeliveryZones sdz
            WHERE sdz.SupplierId = @SupplierId AND sdz.ZoneId = w.ZoneId
        ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasCoverage
    FROM Warehouses w
    JOIN Organizations o       ON o.OrganizationId = w.OrganizationId
    JOIN OrganizationTypes ot  ON ot.OrganizationTypeId = o.OrganizationTypeId
    WHERE w.WarehouseId = @WarehouseId;
END;
GO
