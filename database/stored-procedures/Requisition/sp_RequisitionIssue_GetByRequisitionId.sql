SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   REQUISITIONISSUE - GET BY REQUISITION ID
   Headers only — the service loops per-issue to hydrate Lines via
   sp_RequisitionIssueLine_GetByRequisitionIssueId, same shape as
   InternalOrderService.GetByTokenAsync's own Shipments/Receipts hydration
   (acceptable N+1 here: a Requisition realistically has a handful of issue
   events, never an unbounded cross-organization browse).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_RequisitionIssue_GetByRequisitionId
(
    @RequisitionId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ri.RequisitionIssueId, ri.RequisitionIssueToken, ri.RequisitionId, r.RequisitionToken,
        ri.Notes, ri.CreatedUtc, ri.CreatedBy
    FROM dbo.RequisitionIssues ri
    JOIN dbo.Requisitions r ON r.RequisitionId = ri.RequisitionId
    WHERE ri.RequisitionId = @RequisitionId
    ORDER BY ri.CreatedUtc;
END;
GO
