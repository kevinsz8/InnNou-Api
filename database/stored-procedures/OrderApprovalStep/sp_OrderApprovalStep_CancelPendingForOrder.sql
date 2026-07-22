-- Used by OrderService.CancelAsync when withdrawing an Order directly from PENDING_APPROVAL
-- (as opposed to a step Reject, which cancels siblings as part of sp_OrderApprovalStep_Reject).
CREATE OR ALTER PROCEDURE sp_OrderApprovalStep_CancelPendingForOrder
    @OrderId   INT,
    @DecidedBy VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE OrderApprovalSteps
    SET    OrderApprovalStepStatusId = (SELECT OrderApprovalStepStatusId FROM OrderApprovalStepStatuses WHERE Code = 'CANCELLED'),
           DecidedUtc                = SYSUTCDATETIME(),
           DecidedBy                 = @DecidedBy
    WHERE  OrderId = @OrderId
      AND  OrderApprovalStepStatusId = (SELECT OrderApprovalStepStatusId FROM OrderApprovalStepStatuses WHERE Code = 'PENDING');
END;
GO
