-- Full history for OrderDto.ApprovalSteps — same "second query, always populated" pattern as
-- sp_OrderLine_GetByOrderId feeding OrderDto.Lines. Includes terminal (REJECTED/CANCELLED) rows
-- from past attempts too, so the Order detail view can show the full approval history.
CREATE OR ALTER PROCEDURE sp_OrderApprovalStep_GetByOrderId
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;

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
    WHERE s.OrderId = @OrderId
    ORDER BY s.FamilyCode, s.Level, s.CreatedUtc;
END;
GO
