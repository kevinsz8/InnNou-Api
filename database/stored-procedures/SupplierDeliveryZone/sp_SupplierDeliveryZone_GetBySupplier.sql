CREATE OR ALTER PROCEDURE sp_SupplierDeliveryZone_GetBySupplier
    @SupplierId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        sdz.SupplierDeliveryZoneId, sdz.SupplierDeliveryZoneToken,
        sdz.SupplierId, s.SupplierToken, s.Name AS SupplierName,
        sdz.ZoneId, z.ZoneToken, z.Code AS ZoneCode, z.Name AS ZoneName,
        c.Code AS CountryCode, c.Name AS CountryName,
        sdz.DayOfWeek, sdz.CreatedUtc, sdz.CreatedBy
    FROM SupplierDeliveryZones sdz
    JOIN Suppliers s ON s.SupplierId = sdz.SupplierId
    JOIN Zones     z ON z.ZoneId     = sdz.ZoneId
    JOIN Countries c ON c.CountryId  = z.CountryId
    WHERE sdz.SupplierId = @SupplierId
    ORDER BY c.Name, z.Name, sdz.DayOfWeek;
END;
GO
