-- =============================================================
-- MIGRATION: Drop dead stored procedures (cleanup pass)
-- Date: 2026-07-29
-- =============================================================
-- Two families of leftover procedures found by a codebase-wide dead-code
-- audit, both with zero C# callers anywhere in src/:
--   1. Seven "*_GetAll" lookups (Category/Family/SubCategory/SubFamily/
--      UnitConversionRate/UnitOfMeasure/UnitType) — pre-pagination
--      leftovers; the corresponding services only ever gained a
--      GetPagedAsync, never had a GetAllAsync to call these from.
--   2. sp_PurchaseOrderLine_GetByPurchaseOrderId — the raw, never-rectified
--      line read, fully superseded by sp_PurchaseOrderLine_GetEffective
--      once Purchase Order Rectifications shipped (still referenced only
--      in code comments contrasting the two, never actually called).
-- Guarded so it is a no-op if already applied.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.sp_Category_GetAll', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Category_GetAll;
GO

IF OBJECT_ID('dbo.sp_Family_GetAll', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Family_GetAll;
GO

IF OBJECT_ID('dbo.sp_SubCategory_GetAll', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_SubCategory_GetAll;
GO

IF OBJECT_ID('dbo.sp_SubFamily_GetAll', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_SubFamily_GetAll;
GO

IF OBJECT_ID('dbo.sp_UnitConversionRate_GetAll', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UnitConversionRate_GetAll;
GO

IF OBJECT_ID('dbo.sp_UnitOfMeasure_GetAll', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UnitOfMeasure_GetAll;
GO

IF OBJECT_ID('dbo.sp_UnitType_GetAll', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UnitType_GetAll;
GO

IF OBJECT_ID('dbo.sp_PurchaseOrderLine_GetByPurchaseOrderId', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_PurchaseOrderLine_GetByPurchaseOrderId;
GO
