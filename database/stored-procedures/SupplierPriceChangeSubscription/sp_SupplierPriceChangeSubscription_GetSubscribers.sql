SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- =============================================================
-- SUPPLIER PRICE CHANGE SUBSCRIPTION - GET SUBSCRIBERS
-- Called at price-change notification time — who's watching this Supplier. Returns
-- OrganizationId alongside so the caller can resolve each subscriber's effective Article
-- favorites (for the "your favorite got cheaper/more expensive" detailed variant) without a
-- second per-user round-trip.
-- =============================================================
CREATE OR ALTER PROCEDURE sp_SupplierPriceChangeSubscription_GetSubscribers
    @SupplierId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT u.UserId, u.UserToken, u.OrganizationId
    FROM   SupplierPriceChangeSubscriptions sub
    JOIN   Users u ON u.UserId = sub.UserId
    WHERE  sub.SupplierId = @SupplierId
      AND  u.IsDeleted = 0
      AND  u.IsActive  = 1;
END;
GO
