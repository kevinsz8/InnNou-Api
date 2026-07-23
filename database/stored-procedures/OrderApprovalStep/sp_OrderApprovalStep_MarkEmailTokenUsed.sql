SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDERAPPROVALSTEP - MARK EMAIL TOKEN USED
   Marks the single-use anonymous approval-email token as consumed, right
   after the approval it authorized has succeeded — makes a second click
   on the same emailed link (or a link-scanner replay after the human
   already acted) report AlreadyUsed instead of re-approving.
   Gotcha (see CLAUDE.md, "Article pricing"): the SET ANSI_NULLS/
   QUOTED_IDENTIFIER above are required here because OrderApprovalSteps has
   a filtered index (UX_OrderApprovalSteps_EmailApprovalToken).
   ============================================================= */
CREATE OR ALTER PROCEDURE sp_OrderApprovalStep_MarkEmailTokenUsed
    @OrderApprovalStepToken UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE OrderApprovalSteps
    SET    EmailApprovalTokenUsedUtc = SYSUTCDATETIME()
    WHERE  OrderApprovalStepToken = @OrderApprovalStepToken;
END;
GO
