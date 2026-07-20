/* =============================================================
   SUPPLIERDELIVERYZONE - CHECK COVERAGE
   Single-round-trip gate used by OrderService.AddLineAsync. Resolves
   @OrganizationId's own Zone (never the acting/impersonating user's)
   and reports whether @SupplierId has at least one coverage row for
   it. EnforcementActive is false (never blocks) when the organization
   has no ZoneId set yet, or isn't an ASSOCIATE-type org (Super
   Asociado orgs are never zoned/filtered) — day-of-week is
   deliberately not considered here, coverage on ANY day is enough.
   ============================================================= */
CREATE OR ALTER PROCEDURE sp_SupplierDeliveryZone_CheckCoverage
    @SupplierId     INT,
    @OrganizationId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        o.ZoneId AS OrganizationZoneId,
        CASE WHEN o.ZoneId IS NOT NULL AND ot.Code = 'ASSOCIATE'
             THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS EnforcementActive,
        CASE WHEN EXISTS (
            SELECT 1 FROM SupplierDeliveryZones sdz
            WHERE sdz.SupplierId = @SupplierId AND sdz.ZoneId = o.ZoneId
        ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasCoverage
    FROM Organizations o
    JOIN OrganizationTypes ot ON ot.OrganizationTypeId = o.OrganizationTypeId
    WHERE o.OrganizationId = @OrganizationId;
END;
GO
