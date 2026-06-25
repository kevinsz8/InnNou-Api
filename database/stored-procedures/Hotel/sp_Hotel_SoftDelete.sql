/* =============================================================
   HOTEL - SOFT DELETE
   Marks a hotel as deleted and inactive, recording the full
   deleted audit trail. Does not physically remove the row.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Hotel_SoftDelete
(
    @HotelToken UNIQUEIDENTIFIER,
    @DeletedUtc DATETIME2(7),
    @DeletedBy  VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Hotels
    SET
        IsDeleted      = 1,
        IsActive       = 0,
        LastUpdatedUtc = @DeletedUtc,
        LastUpdatedBy  = @DeletedBy,
        DeletedUtc     = @DeletedUtc,
        DeletedBy      = @DeletedBy
    WHERE HotelToken = @HotelToken;
END;
GO
