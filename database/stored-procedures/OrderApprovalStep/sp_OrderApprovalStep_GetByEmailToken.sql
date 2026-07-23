/* =============================================================
   ORDERAPPROVALSTEP - GET BY EMAIL TOKEN
   Looks up a step by its single-use anonymous email-approval token,
   regardless of status/expiry/used state — validity is decided by the
   caller (OrderService), not here, same "plain lookup by an already-known
   token" convention as sp_OrderApprovalStep_GetByToken. Joins
   Organizations/Warehouses like sp_OrderApprovalStep_GetPendingForApprover
   does, since the anonymous confirmation page needs to display them.
   ============================================================= */
CREATE OR ALTER PROCEDURE sp_OrderApprovalStep_GetByEmailToken
    @EmailApprovalToken UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        s.OrderApprovalStepId, s.OrderApprovalStepToken, s.OrderId, ord.OrderToken,
        org.OrganizationToken, org.Name AS OrganizationName,
        w.WarehouseToken, w.Name AS WarehouseName,
        s.FamilyId, s.FamilyCode, s.Level, s.ThresholdAmount, s.ActualFamilyAmount, s.CurrencyCode,
        s.ApproverUserId, u.UserToken AS ApproverUserToken, u.FirstName + ' ' + u.LastName AS ApproverName,
        oass.Code AS Status, s.DecidedUtc, s.DecidedBy, s.RejectionReason,
        s.EmailApprovalToken, s.EmailApprovalTokenExpiresUtc, s.EmailApprovalTokenUsedUtc,
        s.CreatedUtc, s.CreatedBy
    FROM OrderApprovalSteps s
    JOIN [Order] ord       ON ord.OrderId        = s.OrderId
    JOIN Organizations org ON org.OrganizationId = ord.OrganizationId
    JOIN Warehouses w      ON w.WarehouseId      = ord.WarehouseId
    JOIN Users u           ON u.UserId           = s.ApproverUserId
    JOIN OrderApprovalStepStatuses oass ON oass.OrderApprovalStepStatusId = s.OrderApprovalStepStatusId
    WHERE s.EmailApprovalToken = @EmailApprovalToken;
END;
GO
