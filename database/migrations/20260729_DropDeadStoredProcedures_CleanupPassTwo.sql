-- =============================================================
-- MIGRATION: Drop dead stored procedures (cleanup pass 2)
-- Date: 2026-07-29
-- =============================================================
-- Four more procedures found with zero real callers, all confirmed by
-- code archaeology (not just a string-literal grep) before removal:
--   1. sp_SubCategory_GetByCode — added alongside sp_Category_GetByCode
--      for architectural symmetry (both landed in the same commit,
--      "Articles and families bulk excel flow"), but unlike its sibling
--      it never gained a real caller: no bulk-import flow anywhere in
--      this codebase resolves a "CategoryCode"+"SubCategoryCode" pair
--      via Excel (Article's own bulk import only resolves Family/
--      SubFamily; Article Classification's later "bulk" feature takes
--      already-resolved ids from a UI picker, not Excel text columns).
--   2. sp_Role_GetByToken — built for src/pages/Roles.tsx's edit flow
--      in InnNou-Web, which was entirely commented out and deleted in
--      an earlier dead-code cleanup pass this same day. The DTO it
--      returns has no field beyond what the Roles list already carries,
--      so there's no remaining drill-down it could back either.
--   3. sp_PurchaseOrderRectification_GetByToken — the list endpoint
--      (sp_PurchaseOrderRectification_GetPaged, still in use) already
--      returns full line detail per rectification; RectifyPurchaseOrderModal.tsx
--      renders straight from that list with no click-through drill-down.
--   4. sp_GoodsReceipt_GetByToken — same shape: ReceiveGoodsModal.tsx
--      renders full line detail straight from the GoodsReceipts list.
-- The child-line SPs these last two called (sp_PurchaseOrderLineRectification_
-- GetByRectificationId, sp_GoodsReceiptLine_GetByGoodsReceiptId) are still
-- used elsewhere and are NOT touched by this migration.
-- Guarded so it is a no-op if already applied.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.sp_SubCategory_GetByCode', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_SubCategory_GetByCode;
GO

IF OBJECT_ID('dbo.sp_Role_GetByToken', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Role_GetByToken;
GO

IF OBJECT_ID('dbo.sp_PurchaseOrderRectification_GetByToken', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_PurchaseOrderRectification_GetByToken;
GO

IF OBJECT_ID('dbo.sp_GoodsReceipt_GetByToken', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GoodsReceipt_GetByToken;
GO
