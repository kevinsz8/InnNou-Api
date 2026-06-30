/* =============================================================
   HOTEL CONTACT - SOFT DELETE
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_HotelContact_SoftDelete
(
    @HotelContactToken UNIQUEIDENTIFIER,
    @DeletedUtc        DATETIME2,
    @DeletedBy         VARCHAR(150) = NULL,
    @LastUpdatedUtc    DATETIME2,
    @LastUpdatedBy     VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.HotelContacts
    SET
        IsActive       = 0,
        IsDeleted      = 1,
        DeletedUtc     = @DeletedUtc,
        DeletedBy      = @DeletedBy,
        LastUpdatedUtc = @LastUpdatedUtc,
        LastUpdatedBy  = @LastUpdatedBy
    WHERE HotelContactToken = @HotelContactToken
      AND IsDeleted = 0;
END;
GO
