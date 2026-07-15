SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   WAREHOUSE CONTACT - UPDATE
   Mirrors sp_Supplier_Update: HasAccessToSystem lives on this row as
   the source of truth, updated in lockstep with the linked shadow
   user's credentials/IsActive via sp_User_SetWarehouseContactAccess
   in the same transaction (see WarehouseContactService.EditAsync's
   touchesAccess path).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_WarehouseContact_Update
(
    @WarehouseContactToken UNIQUEIDENTIFIER,
    @ContactName            VARCHAR(150),
    @ContactType            VARCHAR(100) = NULL,
    @Department             VARCHAR(100) = NULL,
    @Phone                  VARCHAR(50)  = NULL,
    @Mobile                 VARCHAR(50)  = NULL,
    @Fax                    VARCHAR(50)  = NULL,
    @Email                  VARCHAR(320) = NULL,
    @Notes                  VARCHAR(500) = NULL,
    @IsPrimary              BIT,
    @HasAccessToSystem      BIT,
    @LastUpdatedUtc         DATETIME2,
    @LastUpdatedBy          VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.WarehouseContacts
    SET
        ContactName        = @ContactName,
        ContactType        = @ContactType,
        Department         = @Department,
        Phone              = @Phone,
        Mobile             = @Mobile,
        Fax                = @Fax,
        Email              = @Email,
        Notes              = @Notes,
        IsPrimary          = @IsPrimary,
        HasAccessToSystem  = @HasAccessToSystem,
        LastUpdatedUtc     = @LastUpdatedUtc,
        LastUpdatedBy      = @LastUpdatedBy
    WHERE WarehouseContactToken = @WarehouseContactToken
      AND IsDeleted = 0;

    SELECT
        WarehouseContactId, WarehouseContactToken, WarehouseId, ContactName, ContactType, Department,
        Phone, Mobile, Fax, Email, Notes, IsPrimary, HasAccessToSystem,
        IsActive, IsDeleted, CreatedUtc, CreatedBy, LastUpdatedUtc, LastUpdatedBy
    FROM dbo.WarehouseContacts
    WHERE WarehouseContactToken = @WarehouseContactToken;
END;
GO
