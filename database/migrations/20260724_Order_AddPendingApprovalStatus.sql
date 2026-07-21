SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   MIGRATION: Order.Status gains PENDING_APPROVAL
   New 4th literal for the Order approval workflow (see
   20260724_FamilyApprovalThresholds_Create.sql / OrderApprovalSteps_Create.sql).
   DRAFT -> PENDING_APPROVAL (Submit attempt crossed a configured Family
   threshold) -> SUBMITTED (auto, once every required step is APPROVED) or
   -> DRAFT (any step REJECTED). CancelAsync also allows cancelling directly
   from PENDING_APPROVAL.

   Idempotent — safe to re-run.
   ============================================================= */

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Order_Status' AND parent_object_id = OBJECT_ID('[Order]'))
BEGIN
    ALTER TABLE [Order] DROP CONSTRAINT CK_Order_Status;
END
GO

ALTER TABLE [Order] ADD CONSTRAINT CK_Order_Status
    CHECK (Status IN (N'DRAFT', N'PENDING_APPROVAL', N'SUBMITTED', N'CANCELLED'));
GO

PRINT '=== Migration 20260724_Order_AddPendingApprovalStatus completed successfully ===';
GO
