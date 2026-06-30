/* =============================================================
   HOTEL CONTACT - CREATE
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_HotelContact_Create
(
    @HotelContactToken UNIQUEIDENTIFIER,
    @HotelId           INT,
    @ContactName       VARCHAR(150),
    @ContactType       VARCHAR(100) = NULL,
    @Department        VARCHAR(100) = NULL,
    @Phone             VARCHAR(50)  = NULL,
    @Mobile            VARCHAR(50)  = NULL,
    @Fax               VARCHAR(50)  = NULL,
    @Email             VARCHAR(320) = NULL,
    @Notes             VARCHAR(500) = NULL,
    @IsPrimary         BIT,
    @CreatedUtc        DATETIME2,
    @CreatedBy         VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.HotelContacts
    (
        HotelContactToken, HotelId, ContactName, ContactType, Department,
        Phone, Mobile, Fax, Email, Notes, IsPrimary,
        IsActive, IsDeleted, CreatedUtc, CreatedBy
    )
    VALUES
    (
        @HotelContactToken, @HotelId, @ContactName, @ContactType, @Department,
        @Phone, @Mobile, @Fax, @Email, @Notes, @IsPrimary,
        1, 0, @CreatedUtc, @CreatedBy
    );

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
