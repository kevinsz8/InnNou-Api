SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- =============================================================
-- SUPPLIER PRICE CHANGE SUBSCRIPTION - GET FOR USER
-- Backs the /preferences page's "which suppliers am I watching" list.
-- =============================================================
CREATE OR ALTER PROCEDURE sp_SupplierPriceChangeSubscription_GetForUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        sub.SupplierPriceChangeSubscriptionId,
        sub.SupplierPriceChangeSubscriptionToken,
        s.SupplierId, s.SupplierToken, s.Name AS SupplierName,
        sub.CreatedUtc
    FROM   SupplierPriceChangeSubscriptions sub
    JOIN   Suppliers s ON s.SupplierId = sub.SupplierId
    WHERE  sub.UserId = @UserId
      AND  s.IsDeleted = 0
    ORDER BY s.Name;
END;
GO
