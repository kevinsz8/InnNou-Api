-- =============================================================
-- MIGRATION: Drop unused Dashboard procedures
-- Date: 2026-07-29
-- =============================================================
-- Replaces the Home dashboard's order-count-by-month-by-status line
-- chart and Active Users tile with an "open POs awaiting receipt"
-- count and a "top 5 suppliers by spend" list (see
-- sp_Dashboard_GetOpenPurchaseOrdersCount / sp_Dashboard_GetTopSuppliersBySpend).
-- The status chart bucketed by creation month + CURRENT status, not
-- a real transition timeline (no per-status timestamp exists in the
-- schema) — confusing and low-signal compared to a plain open-PO
-- count. Active Users was an admin metric, not an operational one;
-- neither had any other consumer. Guarded so it is a no-op if
-- already applied.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.sp_Dashboard_GetOrderCountByMonthByStatus', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Dashboard_GetOrderCountByMonthByStatus;
GO

IF OBJECT_ID('dbo.sp_Dashboard_GetActiveUserSummary', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Dashboard_GetActiveUserSummary;
GO
