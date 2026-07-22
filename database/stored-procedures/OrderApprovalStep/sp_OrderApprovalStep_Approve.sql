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

    IF NOT EXISTS (
        SELECT 1 FROM OrderApprovalSteps s
        JOIN OrderApprovalStepStatuses oass ON oass.OrderApprovalStepStatusId = s.OrderApprovalStepStatusId
        WHERE s.OrderApprovalStepToken = @OrderApprovalStepToken AND oass.Code = 'PENDING'
    )
    BEGIN
        RAISERROR('ORDER_APPROVAL_STEP_ALREADY_DECIDED', 16, 1);
        RETURN;
    END

    UPDATE OrderApprovalSteps
    SET    OrderApprovalStepStatusId = (SELECT OrderApprovalStepStatusId FROM OrderApprovalStepStatuses WHERE Code = 'APPROVED'),
           DecidedUtc                = SYSUTCDATETIME(),
           DecidedBy                 = @DecidedBy
    WHERE  OrderApprovalStepToken = @OrderApprovalStepToken;

    SELECT
        s.OrderApprovalStepId, s.OrderApprovalStepToken, s.OrderId, ord.OrderToken,
        s.FamilyId, s.FamilyCode, s.Level, s.ThresholdAmount, s.ActualFamilyAmount, s.CurrencyCode,
        s.ApproverUserId, u.UserToken AS ApproverUserToken, u.FirstName + ' ' + u.LastName AS ApproverName,
        oass.Code AS Status, s.DecidedUtc, s.DecidedBy, s.RejectionReason,
        s.CreatedUtc, s.CreatedBy
    FROM OrderApprovalSteps s
    JOIN [Order] ord ON ord.OrderId = s.OrderId
    JOIN Users u      ON u.UserId   = s.ApproverUserId
    JOIN OrderApprovalStepStatuses oass ON oass.OrderApprovalStepStatusId = s.OrderApprovalStepStatusId
    WHERE s.OrderApprovalStepToken = @OrderApprovalStepToken;
END;
GO
