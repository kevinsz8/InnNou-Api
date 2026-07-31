-- =============================================================
-- MIGRATION: Add SupplierInvoicePurchaseOrderPolicies (multi-PO-per-invoice
-- toggle, per Organization, hierarchy-inherited)
-- Date: 2026-08-01
-- =============================================================
-- Same nearest-organization-wins hierarchy override shape as
-- SupplierInvoiceMatchTolerances (see 20260730_SupplierInvoices_Create.sql):
-- a Super Asociado sets its own row as the default for the whole tree; an
-- Asociado's own row (if present) takes priority for itself. One row per
-- Organization. Named "PurchaseOrderPolicies" rather than reusing
-- "Consolidation" to avoid colliding with the unrelated
-- ConsolidatedPurchaseOrder module's own terminology.
--
-- Absence of any row in the caller's ancestry (checked in
-- SupplierInvoiceService.GetEffectivePurchaseOrderPolicyAsync) must resolve
-- to "allowed" — the practice today, before this setting existed, is
-- unrestricted multi-PO consolidation, and nothing should silently start
-- rejecting invoice creation for an organization that has never touched
-- this setting.
-- =============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SupplierInvoicePurchaseOrderPolicies')
BEGIN
    CREATE TABLE SupplierInvoicePurchaseOrderPolicies
    (
        SupplierInvoicePurchaseOrderPolicyId    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SupplierInvoicePurchaseOrderPolicyToken UNIQUEIDENTIFIER  NOT NULL DEFAULT NEWID(),
        OrganizationId                          INT               NOT NULL,
        AllowMultiplePurchaseOrders             BIT               NOT NULL,
        CreatedUtc                              DATETIME2         NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                               VARCHAR(150)      NOT NULL,
        LastUpdatedUtc                          DATETIME2         NULL,
        LastUpdatedBy                           VARCHAR(150)      NULL,

        CONSTRAINT UQ_SupplierInvoicePurchaseOrderPolicies_Token UNIQUE (SupplierInvoicePurchaseOrderPolicyToken),
        CONSTRAINT UQ_SupplierInvoicePurchaseOrderPolicies_OrganizationId UNIQUE (OrganizationId),
        CONSTRAINT FK_SupplierInvoicePurchaseOrderPolicies_Organizations FOREIGN KEY (OrganizationId) REFERENCES Organizations (OrganizationId)
    );
END
GO

PRINT '=== Migration 20260801_SupplierInvoicePurchaseOrderPolicies_Create completed successfully ===';
GO
