SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- Rejecting ANY step reverts the whole Order — every other still-PENDING step for the same
-- OrderId is cancelled in the same call, so a fresh Submit attempt later starts a clean batch
-- (no separate "batch id" needed — see 20260724_OrderApprovalSteps_Create.sql's header note).
CREATE OR ALTER PROCEDURE sp_OrderApprovalStep_Reject
    @OrderApprovalStepToken UNIQUEIDENTIFIER,
    @RejectionReason        NVARCHAR(500),
    @DecidedBy              VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PendingStatusId INT = (SELECT OrderApprovalStepStatusId FROM OrderApprovalStepStatuses WHERE Code = 'PENDING');
    DECLARE @OrderId INT;
    SELECT @OrderId = OrderId FROM OrderApprovalSteps WHERE OrderApprovalStepToken = @OrderApprovalStepToken AND OrderApprovalStepStatusId = @PendingStatusId;

    IF @OrderId IS NULL
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
    WHERE  OrderId = @OrderId
      AND  OrderApprovalStepStatusId = @PendingStatusId;

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
