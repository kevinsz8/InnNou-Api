SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   WAREHOUSE CONTACT - GET BY TOKEN
   Joins Warehouses purely to surface the owning Warehouse's
   OrganizationId (as WarehouseOrganizationId) — lets
   WarehouseContactService run its organization-hierarchy
   authorization check off this single row, without a second
   round trip through sp_Warehouse_GetByToken.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_WarehouseContact_GetByToken
(
    @WarehouseContactToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        wc.WarehouseContactId, wc.WarehouseContactToken, wc.WarehouseId, wc.ContactName, wc.ContactType, wc.Department,
        wc.Phone, wc.Mobile, wc.Fax, wc.Email, wc.Notes, wc.IsPrimary, wc.HasAccessToSystem,
        wc.IsActive, wc.IsDeleted, wc.CreatedUtc, wc.CreatedBy, wc.LastUpdatedUtc, wc.LastUpdatedBy, wc.DeletedUtc, wc.DeletedBy,
        w.OrganizationId AS WarehouseOrganizationId
    FROM dbo.WarehouseContacts wc
    INNER JOIN dbo.Warehouses w ON w.WarehouseId = wc.WarehouseId
    WHERE wc.WarehouseContactToken = @WarehouseContactToken
      AND wc.IsDeleted = 0;
END;
GO
