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

    DECLARE @OrderId INT;
    SELECT @OrderId = OrderId FROM OrderApprovalSteps WHERE OrderApprovalStepToken = @OrderApprovalStepToken AND Status = 'PENDING';

    IF @OrderId IS NULL
    BEGIN
        RAISERROR('ORDER_APPROVAL_STEP_ALREADY_DECIDED', 16, 1);
        RETURN;
    END

    UPDATE OrderApprovalSteps
    SET    Status           = 'REJECTED',
           DecidedUtc       = SYSUTCDATETIME(),
           DecidedBy        = @DecidedBy,
           RejectionReason  = @RejectionReason
    WHERE  OrderApprovalStepToken = @OrderApprovalStepToken;

    UPDATE OrderApprovalSteps
    SET    Status     = 'CANCELLED',
           DecidedUtc = SYSUTCDATETIME(),
           DecidedBy  = @DecidedBy
    WHERE  OrderId = @OrderId
      AND  Status  = 'PENDING';

    SELECT
        s.OrderApprovalStepId, s.OrderApprovalStepToken, s.OrderId, ord.OrderToken,
        s.FamilyId, s.FamilyCode, s.Level, s.ThresholdAmount, s.ActualFamilyAmount, s.CurrencyCode,
        s.ApproverUserId, u.UserToken AS ApproverUserToken, u.FirstName + ' ' + u.LastName AS ApproverName,
        s.Status, s.DecidedUtc, s.DecidedBy, s.RejectionReason,
        s.CreatedUtc, s.CreatedBy
    FROM OrderApprovalSteps s
    JOIN [Order] ord ON ord.OrderId = s.OrderId
    JOIN Users u      ON u.UserId   = s.ApproverUserId
    WHERE s.OrderApprovalStepToken = @OrderApprovalStepToken;
END;
GO
