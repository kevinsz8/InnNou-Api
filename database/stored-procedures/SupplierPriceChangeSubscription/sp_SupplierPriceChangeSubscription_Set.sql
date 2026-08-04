SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- =============================================================
-- SUPPLIER PRICE CHANGE SUBSCRIPTION - SET (full replace, one call)
-- Reconciles the caller's complete subscribed-supplier set in one shot — the frontend is a
-- multi-select, not a per-supplier toggle, so "set" (not individual add/remove) is the natural
-- contract. @SupplierTokens is resolved to SupplierIds here (no separate round-trip); any token
-- that doesn't resolve to a real, non-deleted Supplier is silently ignored rather than erroring —
-- visibility (can this user's org even see this Supplier) is enforced by the calling service
-- BEFORE this proc is invoked (CLAUDE.md: organization-hierarchy scoping belongs in the service,
-- never assumed from caller input), this proc trusts its input.
-- =============================================================
CREATE OR ALTER PROCEDURE sp_SupplierPriceChangeSubscription_Set
    @UserId         INT,
    @SupplierTokens NVARCHAR(MAX) = NULL,
    @CreatedBy      VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    CREATE TABLE #TargetSuppliers (SupplierId INT NOT NULL PRIMARY KEY);

    INSERT INTO #TargetSuppliers (SupplierId)
    SELECT DISTINCT s.SupplierId
    FROM   Suppliers s
    WHERE  @SupplierTokens IS NOT NULL
      AND  s.SupplierToken IN (SELECT TRY_CAST(value AS UNIQUEIDENTIFIER) FROM STRING_SPLIT(@SupplierTokens, ','))
      AND  s.IsDeleted = 0;

    BEGIN TRANSACTION;

    DELETE FROM SupplierPriceChangeSubscriptions
    WHERE  UserId = @UserId
      AND  SupplierId NOT IN (SELECT SupplierId FROM #TargetSuppliers);

    INSERT INTO SupplierPriceChangeSubscriptions (UserId, SupplierId, CreatedBy)
    SELECT @UserId, ts.SupplierId, @CreatedBy
    FROM   #TargetSuppliers ts
    WHERE  NOT EXISTS (
        SELECT 1 FROM SupplierPriceChangeSubscriptions x
        WHERE x.UserId = @UserId AND x.SupplierId = ts.SupplierId
    );

    COMMIT TRANSACTION;
END;
GO
