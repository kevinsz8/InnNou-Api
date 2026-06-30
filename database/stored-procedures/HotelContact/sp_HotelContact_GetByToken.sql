/* =============================================================
   HOTEL CONTACT - GET BY TOKEN
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_HotelContact_GetByToken
(
    @HotelContactToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        HotelContactId,
        HotelContactToken,
        HotelId,
        ContactName,
        ContactType,
        Department,
        Phone,
        Mobile,
        Fax,
        Email,
        Notes,
        IsPrimary,
        IsActive,
        IsDeleted,
        CreatedUtc,
        CreatedBy,
        LastUpdatedUtc,
        LastUpdatedBy,
        DeletedUtc,
        DeletedBy
    FROM dbo.HotelContacts
    WHERE HotelContactToken = @HotelContactToken
      AND IsDeleted = 0;
END;
GO
