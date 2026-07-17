SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDERTEMPLATE - CREATE
   Creates a new, empty template against a resolved Warehouse. OwnerUserId
   is resolved by the caller (OrderTemplateService, via sp_User_GetByToken
   against context.EffectiveUserToken) — never derived here.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_OrderTemplate_Create
(
    @OrderTemplateToken UNIQUEIDENTIFIER,
    @Name               NVARCHAR(200),
    @OrganizationId     INT,
    @WarehouseId        INT,
    @OwnerUserId        INT,
    @CreatedBy          VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.OrderTemplate (OrderTemplateToken, Name, OrganizationId, WarehouseId, OwnerUserId, CreatedBy)
    VALUES (@OrderTemplateToken, @Name, @OrganizationId, @WarehouseId, @OwnerUserId, @CreatedBy);

    SELECT
        ot.OrderTemplateId, ot.OrderTemplateToken, ot.Name,
        ot.OrganizationId, org.OrganizationToken,
        ot.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName, w.IsActive AS IsWarehouseActive,
        ot.OwnerUserId, u.UserToken AS OwnerUserToken, u.FirstName AS OwnerFirstName, u.LastName AS OwnerLastName, u.Email AS OwnerEmail,
        ot.CreatedUtc, ot.CreatedBy, ot.LastUpdatedUtc, ot.LastUpdatedBy,
        0 AS LineCount
    FROM dbo.OrderTemplate ot
    JOIN dbo.Organizations org ON org.OrganizationId = ot.OrganizationId
    JOIN dbo.Warehouses    w   ON w.WarehouseId      = ot.WarehouseId
    JOIN dbo.Users         u   ON u.UserId           = ot.OwnerUserId
    WHERE ot.OrderTemplateToken = @OrderTemplateToken;
END;
GO
