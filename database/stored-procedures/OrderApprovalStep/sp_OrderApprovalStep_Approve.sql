SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- Authorization (is this the designated approver, or SuperAdmin) and the "your turn" gate
-- (no lower, still-non-APPROVED sibling Level for the same Order+Family) are enforced by
-- OrderService in C# before this is called — this SP only guards against double-deciding an
-- already-terminal step under a race.
CREATE OR ALTER PROCEDURE sp_OrderApprovalStep_Approve
    @OrderApprovalStepToken UNIQUEIDENTIFIER,
    @DecidedBy              VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PendingStatusId INT = (SELECT OrderApprovalStepStatusId FROM OrderApprovalStepStatuses WHERE Code = 'PENDING');

    -- Not PENDING (already decided, or the token doesn't exist) — return with no result set
    -- rather than RAISERROR. C# maps the resulting null to a 409
    -- ORDER_APPROVAL_STEP_ALREADY_DECIDED instead of an unhandled 500.
    IF NOT EXISTS (
        SELECT 1 FROM OrderApprovalSteps
        WHERE OrderApprovalStepToken = @OrderApprovalStepToken AND OrderApprovalStepStatusId = @PendingStatusId
    )
    BEGIN
        RETURN;
    END

    -- The actual atomic guard against two concurrent Approve calls racing on the same step:
    -- the WHERE clause re-checks PENDING so only the caller that "wins" the row lock flips it
    -- (the IF NOT EXISTS above is just a fast-path early-out, not what makes this safe).
    UPDATE OrderApprovalSteps
    SET    OrderApprovalStepStatusId = (SELECT OrderApprovalStepStatusId FROM OrderApprovalStepStatuses WHERE Code = 'APPROVED'),
           DecidedUtc                = SYSUTCDATETIME(),
           DecidedBy                 = @DecidedBy
    WHERE  OrderApprovalStepToken = @OrderApprovalStepToken
      AND  OrderApprovalStepStatusId = @PendingStatusId;

    IF @@ROWCOUNT = 0
    BEGIN
        RETURN;
    END

    SELECT
        s.OrderApprovalStepId, s.OrderApprovalStepToken, s.OrderId, ord.OrderToken,
        s.FamilyId, s.FamilyCode, s.Level, s.ThresholdAmount, s.ActualFamilyAmount, s.CurrencyCode,
        s.ApproverUserId, u.UserToken AS ApproverUserToken, u.FirstName + ' ' + u.LastName AS ApproverName,
        oass.Code AS Status, s.DecidedUtc, s.DecidedBy, s.RejectionReason,
        s.CreatedUtc, s.CreatedBy, s.TriggeringPurchaseOrderRectificationId
    FROM OrderApprovalSteps s
    JOIN [Order] ord ON ord.OrderId = s.OrderId
    JOIN Users u      ON u.UserId   = s.ApproverUserId
    JOIN OrderApprovalStepStatuses oass ON oass.OrderApprovalStepStatusId = s.OrderApprovalStepStatusId
    WHERE s.OrderApprovalStepToken = @OrderApprovalStepToken;
END;
GO
