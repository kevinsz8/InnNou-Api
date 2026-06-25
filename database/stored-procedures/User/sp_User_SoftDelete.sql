/* =============================================================
   USER - SOFT DELETE
   Marks a user as deleted and inactive, recording the full
   deleted audit trail. Does not physically remove the row.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_User_SoftDelete
(
    @UserToken      UNIQUEIDENTIFIER,
    @IsDeleted      BIT,
    @DeletedUtc     DATETIME2(7),
    @DeletedBy      VARCHAR(150),
    @LastUpdatedUtc DATETIME2(7),
    @LastUpdatedBy  VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
    SET
        IsDeleted      = @IsDeleted,
        IsActive       = 0,
        DeletedUtc     = @DeletedUtc,
        DeletedBy      = @DeletedBy,
        LastUpdatedUtc = @LastUpdatedUtc,
        LastUpdatedBy  = @LastUpdatedBy
    WHERE UserToken = @UserToken;
END;
GO
