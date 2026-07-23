/* =============================================================
   ORDERAPPROVALSTEP - GET PENDING FOR APPROVER
   Only steps that are actually ACTIONABLE right now: PENDING, assigned to
   this user, AND no lower-Level sibling for the same (OrderId, FamilyId)
   is still non-APPROVED — i.e. it's genuinely this approver's turn in the
   sequential chain, not just a future level waiting behind an earlier one.
   ============================================================= */
CREATE OR ALTER PROCEDURE sp_OrderApprovalStep_GetPendingForApprover
    @ApproverUserId INT,
    @PageNumber     INT,
    @PageSize       INT
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
        s.CreatedUtc, s.CreatedBy, s.TriggeringPurchaseOrderRectificationId,
        COUNT(*) OVER() AS TotalCount
    FROM OrderApprovalSteps s
    JOIN [Order] ord      ON ord.OrderId        = s.OrderId
    JOIN Organizations org ON org.OrganizationId = ord.OrganizationId
    JOIN Warehouses w      ON w.WarehouseId      = ord.WarehouseId
    JOIN Users u           ON u.UserId           = s.ApproverUserId
    JOIN OrderApprovalStepStatuses oass ON oass.OrderApprovalStepStatusId = s.OrderApprovalStepStatusId
    WHERE oass.Code = 'PENDING'
      AND s.ApproverUserId = @ApproverUserId
      AND NOT EXISTS (
          SELECT 1 FROM OrderApprovalSteps prior
          JOIN OrderApprovalStepStatuses prior_status ON prior_status.OrderApprovalStepStatusId = prior.OrderApprovalStepStatusId
          WHERE prior.OrderId  = s.OrderId
            AND prior.FamilyId = s.FamilyId
            AND prior.Level    < s.Level
            AND prior_status.Code <> 'APPROVED'
      )
    ORDER BY s.CreatedUtc
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
