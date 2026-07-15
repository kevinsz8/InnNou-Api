SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   USER - SET WAREHOUSE CONTACT ACCESS
   Updates the login fields (Email/UserName/PasswordHash) and
   IsActive flag of the shadow User linked to a WarehouseContact,
   keyed by WarehouseContactId. Purpose-built for the
   WarehouseContacts.HasAccessToSystem toggle flow — mirrors
   sp_User_SetSupplierAccess exactly. Returns the full updated row.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_User_SetWarehouseContactAccess
(
    @WarehouseContactId INT,
    @Email              VARCHAR(320),
    @NormalizedEmail    VARCHAR(320),
    @UserName           VARCHAR(150),
    @NormalizedUserName VARCHAR(150),
    @PasswordHash       VARCHAR(500),
    @IsActive           BIT,
    @LastUpdatedUtc     DATETIME2(7),
    @LastUpdatedBy      VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
    SET
        Email              = @Email,
        NormalizedEmail    = @NormalizedEmail,
        UserName           = @UserName,
        NormalizedUserName = @NormalizedUserName,
        PasswordHash       = @PasswordHash,
        IsActive           = @IsActive,
        LastUpdatedUtc     = @LastUpdatedUtc,
        LastUpdatedBy      = @LastUpdatedBy
    WHERE WarehouseContactId = @WarehouseContactId
      AND IsDeleted = 0;

    SELECT *
    FROM dbo.Users
    WHERE WarehouseContactId = @WarehouseContactId
      AND IsDeleted = 0;
END;
GO
