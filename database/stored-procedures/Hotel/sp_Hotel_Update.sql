/* =============================================================
   HOTEL - UPDATE
   Updates an existing hotel's fields and returns the full
   updated row. Only acts on non-deleted records.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Hotel_Update
(
    @HotelToken     UNIQUEIDENTIFIER,
    @Name           VARCHAR(200),
    @NormalizedName VARCHAR(200),
    @LegalName      VARCHAR(250)  = NULL,
    @Code           VARCHAR(50)   = NULL,
    @ParentHotelId  INT           = NULL,
    @TimeZone       VARCHAR(100)  = NULL,
    @CurrencyCode   VARCHAR(10)   = NULL,
    @LanguageCode   VARCHAR(10)   = NULL,
    @LastUpdatedUtc DATETIME2(7),
    @LastUpdatedBy  VARCHAR(150)  = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Hotels
    SET
        Name           = @Name,
        NormalizedName = @NormalizedName,
        LegalName      = @LegalName,
        Code           = @Code,
        ParentHotelId  = @ParentHotelId,
        TimeZone       = @TimeZone,
        CurrencyCode   = @CurrencyCode,
        LanguageCode   = @LanguageCode,
        LastUpdatedUtc = @LastUpdatedUtc,
        LastUpdatedBy  = @LastUpdatedBy
    WHERE HotelToken = @HotelToken
      AND IsDeleted = 0;

    SELECT
        HotelId, HotelToken, Name, NormalizedName, LegalName, Code,
        ParentHotelId, TimeZone, CurrencyCode, LanguageCode,
        IsActive, IsDeleted, CreatedUtc, CreatedBy,
        LastUpdatedUtc, LastUpdatedBy, DeletedUtc, DeletedBy
    FROM dbo.Hotels
    WHERE HotelToken = @HotelToken;
END;
GO
