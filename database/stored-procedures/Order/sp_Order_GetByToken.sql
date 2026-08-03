SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDER - GET BY TOKEN
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Order_GetByToken
(
    @OrderToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        o.OrderId, o.OrderToken, o.OrganizationId, org.OrganizationToken,
        o.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        os.Code AS Status, o.Notes, o.SubmittedUtc, o.PdfUrl,
        o.CreatedUtc, o.CreatedBy, cu.Email AS CreatedByEmail,
        o.LastUpdatedUtc, o.LastUpdatedBy, luu.Email AS LastUpdatedByEmail
    FROM dbo.[Order] o
    JOIN dbo.Organizations org ON org.OrganizationId = o.OrganizationId
    JOIN dbo.Warehouses    w   ON w.WarehouseId      = o.WarehouseId
    JOIN dbo.OrderStatuses os  ON os.OrderStatusId    = o.OrderStatusId
    -- CreatedBy/LastUpdatedBy are the actor's UserToken (varchar, see CLAUDE.md's audit-trail
    -- convention), never a display identity by themselves — resolve to the account's Email here
    -- so a client-facing activity feed doesn't have to show a raw UserToken.
    LEFT JOIN dbo.Users cu  ON cu.UserToken  = TRY_CAST(o.CreatedBy AS UNIQUEIDENTIFIER)
    LEFT JOIN dbo.Users luu ON luu.UserToken = TRY_CAST(o.LastUpdatedBy AS UNIQUEIDENTIFIER)
    WHERE o.OrderToken = @OrderToken;
END;
GO
