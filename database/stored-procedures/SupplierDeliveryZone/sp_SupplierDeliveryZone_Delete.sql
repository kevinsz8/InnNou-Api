CREATE OR ALTER PROCEDURE sp_SupplierDeliveryZone_Delete
    @SupplierId INT,
    @ZoneId     INT,
    @DayOfWeek  TINYINT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM SupplierDeliveryZones
    WHERE SupplierId = @SupplierId AND ZoneId = @ZoneId AND DayOfWeek = @DayOfWeek;

    SELECT @@ROWCOUNT AS DeletedCount;
END;
GO
