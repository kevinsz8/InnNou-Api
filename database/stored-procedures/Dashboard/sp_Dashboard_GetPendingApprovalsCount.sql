SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   DASHBOARD - GET PENDING APPROVALS COUNT
   Count of OrderApprovalSteps still PENDING across the caller's own
   organization hierarchy — deliberately not filtered by ApproverUserId,
   this is "how many are pending org-wide," not "assigned to me" (that's
   sp_OrderApprovalStep_GetPendingForApprover's job, untouched by this
   module). OrderApprovalSteps has no OrganizationId column of its own;
   scoping goes through [Order].
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Dashboard_GetPendingApprovalsCount
(
    @RootOrganizationId INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH OrganizationHierarchy AS
    (
        SELECT o.OrganizationId
        FROM dbo.Organizations o
        WHERE o.OrganizationId = @RootOrganizationId

        UNION ALL

        SELECT o.OrganizationId
        FROM dbo.Organizations o
        INNER JOIN OrganizationHierarchy oh ON o.ParentOrganizationId = oh.OrganizationId
    )
    SELECT COUNT(*) AS PendingApprovalsCount
    FROM dbo.OrderApprovalSteps s
    JOIN dbo.[Order] ord ON ord.OrderId = s.OrderId
    JOIN dbo.OrderApprovalStepStatuses sts ON sts.OrderApprovalStepStatusId = s.OrderApprovalStepStatusId
    WHERE sts.Code = 'PENDING'
      AND (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = ord.OrganizationId));
END;
GO
