/* =============================================================
   HOTEL CONTACT - UPDATE
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_HotelContact_Update
(
    @HotelContactToken UNIQUEIDENTIFIER,
    @ContactName       VARCHAR(150),
    @ContactType       VARCHAR(100) = NULL,
    @Department        VARCHAR(100) = NULL,
    @Phone             VARCHAR(50)  = NULL,
    @Mobile            VARCHAR(50)  = NULL,
    @Fax               VARCHAR(50)  = NULL,
    @Email             VARCHAR(320) = NULL,
    @Notes             VARCHAR(500) = NULL,
    @IsPrimary         BIT,
    @LastUpdatedUtc    DATETIME2,
    @LastUpdatedBy     VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.HotelContacts
    SET
        ContactName    = @ContactName,
        ContactType    = @ContactType,
        Department     = @Department,
        Phone          = @Phone,
        Mobile         = @Mobile,
        Fax            = @Fax,
        Email          = @Email,
        Notes          = @Notes,
        IsPrimary      = @IsPrimary,
        LastUpdatedUtc = @LastUpdatedUtc,
        LastUpdatedBy  = @LastUpdatedBy
    WHERE HotelContactToken = @HotelContactToken
      AND IsDeleted = 0;

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
        LastUpdatedBy
    FROM dbo.HotelContacts
    WHERE HotelContactToken = @HotelContactToken;
END;
GO
