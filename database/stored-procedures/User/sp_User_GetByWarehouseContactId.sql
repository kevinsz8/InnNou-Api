SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   USER - GET BY WAREHOUSE CONTACT ID
   Returns the shadow User row linked to a WarehouseContact
   (one-to-one), looked up by WarehouseContacts.WarehouseContactId.
   Used when toggling a WarehouseContact's HasAccessToSystem flag.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_User_GetByWarehouseContactId
(
    @WarehouseContactId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.Users
    WHERE WarehouseContactId = @WarehouseContactId
      AND IsDeleted = 0;
END;
GO
