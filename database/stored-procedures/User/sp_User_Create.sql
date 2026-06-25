/* =============================================================
   USER - CREATE
   Inserts a new user and returns the full created row.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_User_Create
(
    @UserToken          UNIQUEIDENTIFIER,
    @FirstName          VARCHAR(150),
    @LastName           VARCHAR(150),
    @Email              VARCHAR(320),
    @NormalizedEmail    VARCHAR(320),
    @UserName           VARCHAR(150),
    @NormalizedUserName VARCHAR(150),
    @PasswordHash       VARCHAR(500),
    @RoleId             INT,
    @HotelId            INT          = NULL,
    @SupplierId         INT          = NULL,
    @IsActive           BIT,
    @IsDeleted          BIT,
    @CreatedUtc         DATETIME2(7),
    @CreatedBy          VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Users
    (
        UserToken, FirstName, LastName, Email, NormalizedEmail,
        UserName, NormalizedUserName, PasswordHash, RoleId,
        HotelId, SupplierId, IsActive, IsDeleted, CreatedUtc, CreatedBy
    )
    VALUES
    (
        @UserToken, @FirstName, @LastName, @Email, @NormalizedEmail,
        @UserName, @NormalizedUserName, @PasswordHash, @RoleId,
        @HotelId, @SupplierId, @IsActive, @IsDeleted, @CreatedUtc, @CreatedBy
    );

    SELECT * FROM dbo.Users WHERE UserToken = @UserToken;
END;
GO
