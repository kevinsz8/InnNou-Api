SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDERAPPROVALSTEP - ISSUE EMAIL TOKEN
   Issues (or reissues) the single-use anonymous approval-email token for a
   step — only when the step is still PENDING (a no-op otherwise; the
   caller decides whether/when it's this step's turn to be emailed).
   Gotcha (see CLAUDE.md, "Article pricing"): the SET ANSI_NULLS/
   QUOTED_IDENTIFIER above are required here because OrderApprovalSteps has
   a filtered index (UX_OrderApprovalSteps_EmailApprovalToken).
   ============================================================= */
CREATE OR ALTER PROCEDURE sp_OrderApprovalStep_IssueEmailToken
    @OrderApprovalStepToken UNIQUEIDENTIFIER,
    @EmailApprovalToken     UNIQUEIDENTIFIER,
    @ExpiresUtc             DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE s
    SET    s.EmailApprovalToken = @EmailApprovalToken,
           s.EmailApprovalTokenExpiresUtc = @ExpiresUtc,
           s.EmailApprovalTokenUsedUtc = NULL
    FROM OrderApprovalSteps s
    JOIN OrderApprovalStepStatuses oass ON oass.OrderApprovalStepStatusId = s.OrderApprovalStepStatusId
    WHERE s.OrderApprovalStepToken = @OrderApprovalStepToken
      AND oass.Code = 'PENDING';
END;
GO
