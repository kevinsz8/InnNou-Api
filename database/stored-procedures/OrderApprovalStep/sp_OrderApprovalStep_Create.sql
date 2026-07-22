SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDERAPPROVALSTEP - CREATE
   Called once per triggered (Family, Level) from OrderService.SubmitAsync's
   evaluation step — same "loop, one call per item" shape as
   sp_PurchaseOrder_Create inside the Submit split. All frozen snapshot
   values (ThresholdAmount/ActualFamilyAmount/ApproverUserId/FamilyCode)
   are resolved by the caller before this call, not re-derived here.
   ============================================================= */
CREATE OR ALTER PROCEDURE sp_OrderApprovalStep_Create
    @OrderApprovalStepToken UNIQUEIDENTIFIER,
    @OrderId                INT,
    @FamilyId               INT,
    @FamilyCode             VARCHAR(50),
    @Level                  TINYINT,
    @ThresholdAmount        DECIMAL(18,4),
    @ActualFamilyAmount     DECIMAL(18,4),
    @CurrencyCode           VARCHAR(3),
    @ApproverUserId         INT,
    @CreatedBy              VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO OrderApprovalSteps
        (OrderApprovalStepToken, OrderId, FamilyId, FamilyCode, Level, ThresholdAmount, ActualFamilyAmount, CurrencyCode, ApproverUserId, OrderApprovalStepStatusId, CreatedBy)
    VALUES
        (@OrderApprovalStepToken, @OrderId, @FamilyId, @FamilyCode, @Level, @ThresholdAmount, @ActualFamilyAmount, @CurrencyCode, @ApproverUserId, (SELECT OrderApprovalStepStatusId FROM OrderApprovalStepStatuses WHERE Code = 'PENDING'), @CreatedBy);

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
