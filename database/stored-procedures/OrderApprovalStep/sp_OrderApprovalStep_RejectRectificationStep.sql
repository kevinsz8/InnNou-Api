SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- Same shape as sp_OrderApprovalStep_Reject, but the sibling-cancel is scoped to
-- TriggeringPurchaseOrderRectificationId instead of the whole OrderId — two different
-- PurchaseOrders split from the same Order can each have their own rectification pending
-- approval at the same time, and rejecting one must never cancel the other's still-pending
-- steps. See .claude/PurchaseOrderRectificationModule.md.
CREATE OR ALTER PROCEDURE sp_OrderApprovalStep_RejectRectificationStep
    @OrderApprovalStepToken UNIQUEIDENTIFIER,
    @RejectionReason        NVARCHAR(500),
    @DecidedBy              VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PendingStatusId INT = (SELECT OrderApprovalStepStatusId FROM OrderApprovalStepStatuses WHERE Code = 'PENDING');
    DECLARE @TriggeringPurchaseOrderRectificationId INT;
    SELECT @TriggeringPurchaseOrderRectificationId = TriggeringPurchaseOrderRectificationId
    FROM OrderApprovalSteps
    WHERE OrderApprovalStepToken = @OrderApprovalStepToken AND OrderApprovalStepStatusId = @PendingStatusId;

    IF @TriggeringPurchaseOrderRectificationId IS NULL
    BEGIN
        RAISERROR('ORDER_APPROVAL_STEP_ALREADY_DECIDED', 16, 1);
        RETURN;
    END

    UPDATE OrderApprovalSteps
    SET    OrderApprovalStepStatusId = (SELECT OrderApprovalStepStatusId FROM OrderApprovalStepStatuses WHERE Code = 'REJECTED'),
           DecidedUtc                = SYSUTCDATETIME(),
           DecidedBy                 = @DecidedBy,
           RejectionReason           = @RejectionReason
    WHERE  OrderApprovalStepToken = @OrderApprovalStepToken;

    UPDATE OrderApprovalSteps
    SET    OrderApprovalStepStatusId = (SELECT OrderApprovalStepStatusId FROM OrderApprovalStepStatuses WHERE Code = 'CANCELLED'),
           DecidedUtc                = SYSUTCDATETIME(),
           DecidedBy                 = @DecidedBy
    WHERE  TriggeringPurchaseOrderRectificationId = @TriggeringPurchaseOrderRectificationId
      AND  OrderApprovalStepStatusId = @PendingStatusId;

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
