SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDERTEMPLATE - RENAME
   Name-only edit — Organization/Warehouse/Owner are fixed at creation,
   same as Order's own Organization/Warehouse never being editable.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_OrderTemplate_Rename
(
    @OrderTemplateToken UNIQUEIDENTIFIER,
    @Name               NVARCHAR(200),
    @LastUpdatedBy      VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.OrderTemplate
    SET
        Name           = @Name,
        LastUpdatedUtc = SYSUTCDATETIME(),
        LastUpdatedBy  = @LastUpdatedBy
    WHERE OrderTemplateToken = @OrderTemplateToken;

    SELECT
        ot.OrderTemplateId, ot.OrderTemplateToken, ot.Name,
        ot.OrganizationId, org.OrganizationToken,
        ot.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName, w.IsActive AS IsWarehouseActive,
        ot.OwnerUserId, u.UserToken AS OwnerUserToken, u.FirstName AS OwnerFirstName, u.LastName AS OwnerLastName, u.Email AS OwnerEmail,
        ot.CreatedUtc, ot.CreatedBy, ot.LastUpdatedUtc, ot.LastUpdatedBy,
        lc.LineCount
    FROM dbo.OrderTemplate ot
    JOIN dbo.Organizations org ON org.OrganizationId = ot.OrganizationId
    JOIN dbo.Warehouses    w   ON w.WarehouseId      = ot.WarehouseId
    JOIN dbo.Users         u   ON u.UserId           = ot.OwnerUserId
    CROSS APPLY (SELECT COUNT(*) AS LineCount FROM dbo.OrderTemplateLine otl WHERE otl.OrderTemplateId = ot.OrderTemplateId) lc
    WHERE ot.OrderTemplateToken = @OrderTemplateToken;
END;
GO
