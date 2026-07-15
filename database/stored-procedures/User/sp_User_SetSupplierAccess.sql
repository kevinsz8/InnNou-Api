SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   USER - SET SUPPLIER ACCESS
   Updates the login fields (Email/UserName/PasswordHash) and
   IsActive flag of the shadow User linked to a Supplier, keyed by
   SupplierId. Purpose-built for the Suppliers.HasAccessToSystem
   toggle flow — distinct from sp_User_Update, which serves the
   general Users CRUD edit flow and has different parameters.
   Returns the full updated row.
   The SET statements above are required because Users has filtered
   unique indexes (UX_Users_OneUserPerSupplier, UX_Users_OneUserPerWarehouseContact)
   — see sp_Supplier_Create's header comment for the full gotcha.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_User_SetSupplierAccess
(
    @SupplierId         INT,
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
    WHERE SupplierId = @SupplierId
      AND IsDeleted = 0;

    SELECT *
    FROM dbo.Users
    WHERE SupplierId = @SupplierId
      AND IsDeleted = 0;
END;
GO
