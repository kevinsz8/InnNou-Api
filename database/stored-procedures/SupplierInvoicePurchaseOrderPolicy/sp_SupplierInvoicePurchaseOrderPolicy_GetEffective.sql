SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERINVOICEPURCHASEORDERPOLICY - GET EFFECTIVE
   Same nearest-organization-wins hierarchy walk as
   sp_SupplierInvoiceMatchTolerance_GetEffective: my own row if configured,
   else my nearest configured ancestor's row. Zero rows means nobody in the
   ancestry has ever configured this — the caller (
   SupplierInvoiceService.GetEffectivePurchaseOrderPolicyAsync) must treat
   that as "allowed" (today's unrestricted default), not as a missing
   required setting.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierInvoicePurchaseOrderPolicy_GetEffective
(
    @OrganizationId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH OrganizationAncestry AS
    (
        SELECT OrganizationId, ParentOrganizationId, 0 AS Depth
        FROM   Organizations
        WHERE  OrganizationId = @OrganizationId
          AND  IsDeleted = 0
          AND  IsActive  = 1

        UNION ALL

        SELECT o.OrganizationId, o.ParentOrganizationId, oa.Depth + 1
        FROM   Organizations o
        INNER JOIN OrganizationAncestry oa ON o.OrganizationId = oa.ParentOrganizationId
        WHERE  o.IsDeleted = 0
          AND  o.IsActive  = 1
    )
    SELECT TOP 1
        pol.SupplierInvoicePurchaseOrderPolicyId,
        pol.SupplierInvoicePurchaseOrderPolicyToken,
        pol.OrganizationId AS EffectiveOrganizationId,
        org.OrganizationToken AS EffectiveOrganizationToken,
        org.Name         AS EffectiveOrganizationName,
        pol.AllowMultiplePurchaseOrders,
        CASE WHEN oa.Depth = 0 THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS IsInherited
    FROM SupplierInvoicePurchaseOrderPolicies pol
    INNER JOIN OrganizationAncestry oa ON oa.OrganizationId = pol.OrganizationId
    INNER JOIN Organizations org       ON org.OrganizationId = pol.OrganizationId
    ORDER BY oa.Depth ASC;
END;
GO
