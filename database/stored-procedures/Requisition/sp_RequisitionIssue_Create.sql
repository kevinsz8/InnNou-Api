SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   REQUISITIONISSUE - CREATE
   Header only — lines are inserted separately via
   sp_RequisitionIssueLine_Create, called in a loop from the same C#
   transaction (mirrors GoodsReceipt/GoodsReceiptLine's own two-SP
   relationship).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_RequisitionIssue_Create
(
    @RequisitionIssueToken UNIQUEIDENTIFIER,
    @RequisitionId           INT,
    @Notes                    NVARCHAR(1000) = NULL,
    @CreatedBy                 VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.RequisitionIssues (RequisitionIssueToken, RequisitionId, Notes, CreatedBy)
    VALUES (@RequisitionIssueToken, @RequisitionId, @Notes, @CreatedBy);

    SELECT
        ri.RequisitionIssueId, ri.RequisitionIssueToken, ri.RequisitionId, r.RequisitionToken,
        ri.Notes, ri.CreatedUtc, ri.CreatedBy
    FROM dbo.RequisitionIssues ri
    JOIN dbo.Requisitions r ON r.RequisitionId = ri.RequisitionId
    WHERE ri.RequisitionIssueToken = @RequisitionIssueToken;
END;
GO
