-- =============================================================
-- MIGRATION: Restructure sidebar into 5 top-level groups
-- Date: 2026-07-25
-- =============================================================
-- groupProcurement had grown to 10 unrelated items (Suppliers, Articles,
-- Warehouses, Article Favorites, Orders, Order Templates, Delivery Zones,
-- Article Classification, Family Approval Thresholds, Pending Approvals) —
-- a flat dumping ground mixing master data, transactional workflow, and
-- financial config. Confirmed against real precedent (Odoo Purchase, Coupa,
-- Procurify/ProcureDesk/Zycus) before restructuring: the recurring pattern
-- across procurement SaaS is Master-Data/Catalog vs Transactional/Orders vs
-- Configuration/Approval-Settings vs Admin as four distinct areas — and
-- "Pending Approvals" (a daily action item) is never bundled with its own
-- threshold *configuration* (a settings concern), even though both are
-- "approval-workflow" features.
--
-- This also finally uses the groupOperations/groupFinance i18n keys
-- (menu.groupOperations/groupFinance + menu.breadcrumbOperations/Finance)
-- that already existed translated in en/es/ca.json but were never wired to
-- an actual MenuItems row — the original IA anticipated 5 groups, only 3
-- ever got built.
--
-- New layout:
--   groupCatalogs   (2): Categories, Families, Unit Types, Units of Measure,
--                        Zones, Article Classification (taxonomy, same
--                        bucket as Categories/Families — confirmed with user)
--   groupProcurement (3): Suppliers, Articles, Delivery Zones (supplier/
--                        product master data + supplier-side config)
--   groupOperations (4, NEW): Warehouses, Orders, Order Templates,
--                        Article Favorites (ordering-prep, confirmed with
--                        user), Pending Approvals (daily action item)
--   groupFinance    (5, NEW): Family Approval Thresholds (spend config)
--   groupAdmin      (6, was 4): Organizations, Users
--
-- No frontend code change needed — DashboardLayout.tsx's buildMenuTree()
-- is fully DB-driven; breadcrumbKeyFor() derives "menu.breadcrumbOperations"
-- from the group Name by convention, and the i18n strings already exist.
--
-- Idempotent — safe to re-run.
-- =============================================================

-- 1. Renumber existing top-level groups to make room, and add the two new ones.
UPDATE MenuItems SET SortOrder = 6 WHERE Name = 'groupAdmin' AND ParentMenuItemId IS NULL;
GO

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT NULL, 'groupOperations', NULL, NULL, 4, 'System'
WHERE NOT EXISTS (SELECT 1 FROM MenuItems WHERE Name = 'groupOperations' AND ParentMenuItemId IS NULL);
GO

INSERT INTO MenuItems (ParentMenuItemId, Name, Route, Icon, SortOrder, CreatedBy)
SELECT NULL, 'groupFinance', NULL, NULL, 5, 'System'
WHERE NOT EXISTS (SELECT 1 FROM MenuItems WHERE Name = 'groupFinance' AND ParentMenuItemId IS NULL);
GO

-- 2. Move Article Classification into groupCatalogs (was groupProcurement).
UPDATE m
SET m.ParentMenuItemId = p.MenuItemId, m.SortOrder = 6
FROM MenuItems m
JOIN MenuItems p ON p.Name = 'groupCatalogs' AND p.ParentMenuItemId IS NULL
WHERE m.Name = 'articleClassification';
GO

-- 3. Move Warehouses, Orders, Order Templates, Article Favorites, Pending
--    Approvals into the new groupOperations (all were groupProcurement).
UPDATE m
SET m.ParentMenuItemId = p.MenuItemId, m.SortOrder = 1
FROM MenuItems m
JOIN MenuItems p ON p.Name = 'groupOperations' AND p.ParentMenuItemId IS NULL
WHERE m.Name = 'warehouses';
GO

UPDATE m
SET m.ParentMenuItemId = p.MenuItemId, m.SortOrder = 2
FROM MenuItems m
JOIN MenuItems p ON p.Name = 'groupOperations' AND p.ParentMenuItemId IS NULL
WHERE m.Name = 'orders';
GO

UPDATE m
SET m.ParentMenuItemId = p.MenuItemId, m.SortOrder = 3
FROM MenuItems m
JOIN MenuItems p ON p.Name = 'groupOperations' AND p.ParentMenuItemId IS NULL
WHERE m.Name = 'orderTemplates';
GO

UPDATE m
SET m.ParentMenuItemId = p.MenuItemId, m.SortOrder = 4
FROM MenuItems m
JOIN MenuItems p ON p.Name = 'groupOperations' AND p.ParentMenuItemId IS NULL
WHERE m.Name = 'articleFavorites';
GO

UPDATE m
SET m.ParentMenuItemId = p.MenuItemId, m.SortOrder = 5
FROM MenuItems m
JOIN MenuItems p ON p.Name = 'groupOperations' AND p.ParentMenuItemId IS NULL
WHERE m.Name = 'pendingApprovals';
GO

-- 4. Move Family Approval Thresholds into the new groupFinance (was groupProcurement).
UPDATE m
SET m.ParentMenuItemId = p.MenuItemId, m.SortOrder = 1
FROM MenuItems m
JOIN MenuItems p ON p.Name = 'groupFinance' AND p.ParentMenuItemId IS NULL
WHERE m.Name = 'familyApprovalThresholds';
GO

-- 5. Renumber what's left in groupProcurement (Suppliers, Articles, Delivery
--    Zones) now that everything else has moved out.
UPDATE MenuItems SET SortOrder = 1 WHERE Name = 'suppliers';
UPDATE MenuItems SET SortOrder = 2 WHERE Name = 'articles';
UPDATE MenuItems SET SortOrder = 3 WHERE Name = 'deliveryZones';
GO

PRINT '=== Migration 20260725_MenuItems_RestructureIntoFiveGroups completed successfully ===';
GO
