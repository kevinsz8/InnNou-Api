SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- SupplierDeliveryZones has a unique index (UX_SupplierDeliveryZones_Supplier_Zone_Day) —
-- INSERT against a table with a unique index requires QUOTED_IDENTIFIER ON at the session
-- that created this procedure (SQL Server compiles that setting into the proc), not just at
-- index creation time. Without this, every insert fails with error 1934.
CREATE OR ALTER PROCEDURE sp_SupplierDeliveryZone_Create
    @SupplierDeliveryZoneToken UNIQUEIDENTIFIER,
    @SupplierId                INT,
    @ZoneId                    INT,
    @DayOfWeek                 TINYINT,
    @CreatedBy                 VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM SupplierDeliveryZones WHERE SupplierId = @SupplierId AND ZoneId = @ZoneId AND DayOfWeek = @DayOfWeek)
            INSERT INTO SupplierDeliveryZones (SupplierDeliveryZoneToken, SupplierId, ZoneId, DayOfWeek, CreatedBy)
            VALUES (@SupplierDeliveryZoneToken, @SupplierId, @ZoneId, @DayOfWeek, @CreatedBy);
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (2601, 2627) THROW;
    END CATCH

    -- Re-select by (SupplierId, ZoneId, DayOfWeek), not @SupplierDeliveryZoneToken — if the row
    -- already existed, the passed-in token was never used; the caller must get back the real,
    -- pre-existing token. This is what makes "mark twice" a no-op success instead of a conflict.
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
    WHERE sdz.SupplierId = @SupplierId AND sdz.ZoneId = @ZoneId AND sdz.DayOfWeek = @DayOfWeek;
END;
GO
