CREATE OR ALTER PROCEDURE sp_Article_GetByTokens
    @ArticleTokens NVARCHAR(MAX) -- comma-separated ArticleToken GUIDs
AS
BEGIN
    SET NOCOUNT ON;

    -- Lean, bypass-mode batch resolver — deliberately NOT a batch version of the full
    -- sp_Article_GetByToken (which layers hierarchy/private-supplier visibility filtering and
    -- Favorites/Classification projection on top). Built for RequisitionService.CreateAsync's own
    -- per-line article-token-resolution loop (2026-08-07 full-system audit finding #9), which
    -- already calls sp_Article_GetByToken with its default bypass params (no OrganizationId /
    -- ContextRoleLevel=100) — no visibility CTE ever runs for that call site, so replicating it
    -- here would be dead weight. InternalOrderService/PurchaseOrderService.RectifyAsync's own
    -- per-line resolution loops call sp_Article_GetByToken WITH OrganizationId/ContextRoleLevel=0
    -- (real hierarchy/private-supplier visibility enforcement) and are deliberately NOT batched
    -- here — duplicating that CTE logic into a STRING_SPLIT batch form is a correctness/security
    -- risk disproportionate to the benefit for a "how many lines are on one order" bounded loop.
    SELECT
        a.ArticleId,
        a.ArticleToken,
        a.SupplierId,
        a.Name,
        a.PurchaseUnitId,
        a.IsActive,
        a.IsDeleted
    FROM Articles a
    WHERE a.ArticleToken IN (SELECT CAST(value AS UNIQUEIDENTIFIER) FROM STRING_SPLIT(@ArticleTokens, ','))
      AND a.IsDeleted = 0;
END;
