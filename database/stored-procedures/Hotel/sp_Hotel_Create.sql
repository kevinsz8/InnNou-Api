/* =============================================================
   HOTEL - CREATE
   Inserts a new hotel and returns the full created row.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Hotel_Create
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
    @IsActive       BIT,
    @IsDeleted      BIT,
    @CreatedUtc     DATETIME2(7),
    @CreatedBy      VARCHAR(150)  = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Hotels
    (
        HotelToken, Name, NormalizedName, LegalName, Code,
        ParentHotelId, TimeZone, CurrencyCode, LanguageCode,
        IsActive, IsDeleted, CreatedUtc, CreatedBy
    )
    VALUES
    (
        @HotelToken, @Name, @NormalizedName, @LegalName, @Code,
        @ParentHotelId, @TimeZone, @CurrencyCode, @LanguageCode,
        @IsActive, @IsDeleted, @CreatedUtc, @CreatedBy
    );

    SELECT
        HotelId, HotelToken, Name, NormalizedName, LegalName, Code,
        ParentHotelId, TimeZone, CurrencyCode, LanguageCode,
        IsActive, IsDeleted, CreatedUtc, CreatedBy,
        LastUpdatedUtc, LastUpdatedBy, DeletedUtc, DeletedBy
    FROM dbo.Hotels
    WHERE HotelToken = @HotelToken;
END;
GO
