SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERINVOICEPURCHASEORDERPOLICY - UPSERT
   Same insert-or-update shape as sp_SupplierInvoiceMatchTolerance_Upsert.
   Always targets @OrganizationId's own row exactly, never an ancestor's —
   authorization (which org the caller may write) is entirely in
   SupplierInvoiceService.UpsertPurchaseOrderPolicyAsync, not here.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierInvoicePurchaseOrderPolicy_Upsert
(
    @OrganizationId              INT,
    @AllowMultiplePurchaseOrders BIT,
    @LastUpdatedBy               VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.SupplierInvoicePurchaseOrderPolicies WHERE OrganizationId = @OrganizationId)
    BEGIN
        UPDATE dbo.SupplierInvoicePurchaseOrderPolicies
        SET AllowMultiplePurchaseOrders = @AllowMultiplePurchaseOrders,
            LastUpdatedUtc              = SYSUTCDATETIME(),
            LastUpdatedBy               = @LastUpdatedBy
        WHERE OrganizationId = @OrganizationId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.SupplierInvoicePurchaseOrderPolicies (OrganizationId, AllowMultiplePurchaseOrders, CreatedBy)
        VALUES (@OrganizationId, @AllowMultiplePurchaseOrders, @LastUpdatedBy);
    END

    SELECT pol.SupplierInvoicePurchaseOrderPolicyId, pol.SupplierInvoicePurchaseOrderPolicyToken,
           pol.OrganizationId AS EffectiveOrganizationId, org.OrganizationToken AS EffectiveOrganizationToken,
           org.Name AS EffectiveOrganizationName,
           pol.AllowMultiplePurchaseOrders, CAST(0 AS BIT) AS IsInherited
    FROM dbo.SupplierInvoicePurchaseOrderPolicies pol
    JOIN dbo.Organizations org ON org.OrganizationId = pol.OrganizationId
    WHERE pol.OrganizationId = @OrganizationId;
END;
GO
