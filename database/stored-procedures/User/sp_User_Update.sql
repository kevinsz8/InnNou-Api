/* =============================================================
   USER - UPDATE
   Updates an existing user's fields and returns the full
   updated row.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_User_Update
(
    @UserToken          UNIQUEIDENTIFIER,
    @Email              VARCHAR(320),
    @NormalizedEmail    VARCHAR(320),
    @FirstName          VARCHAR(150),
    @LastName           VARCHAR(150),
    @UserName           VARCHAR(150),
    @NormalizedUserName VARCHAR(150),
    @PasswordHash       VARCHAR(500),
    @RoleId             INT,
    @HotelId            INT          = NULL,
    @LastUpdatedUtc     DATETIME2(7),
    @LastUpdatedBy      VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
    SET
        Email              = @Email,
        NormalizedEmail    = @NormalizedEmail,
        FirstName          = @FirstName,
        LastName           = @LastName,
        UserName           = @UserName,
        NormalizedUserName = @NormalizedUserName,
        PasswordHash       = @PasswordHash,
        RoleId             = @RoleId,
        HotelId            = @HotelId,
        LastUpdatedUtc     = @LastUpdatedUtc,
        LastUpdatedBy      = @LastUpdatedBy
    WHERE UserToken = @UserToken;

    SELECT * FROM dbo.Users WHERE UserToken = @UserToken;
END;
GO
