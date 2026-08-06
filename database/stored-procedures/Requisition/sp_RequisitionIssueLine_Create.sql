SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   REQUISITIONISSUELINE - CREATE
   The "how much did we hand over this time" fact — closes out against the
   original RequisitionLine directly (no intermediate shipment step, unlike
   Internal Orders, since store-to-department issuance is instantaneous, not
   a multi-day physical shipping process).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_RequisitionIssueLine_Create
(
    @RequisitionIssueLineToken UNIQUEIDENTIFIER,
    @RequisitionIssueId          INT,
    @RequisitionLineId            INT,
    @QuantityIssued                 DECIMAL(18,8),
    @IssuedUnitId                    INT = NULL,
    @IssuedQuantity                  DECIMAL(18,8) = NULL,
    @Notes                           NVARCHAR(500) = NULL,
    @CreatedBy                       VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.RequisitionIssueLines (RequisitionIssueLineToken, RequisitionIssueId, RequisitionLineId, QuantityIssued, IssuedUnitId, IssuedQuantity, Notes, CreatedBy)
    VALUES (@RequisitionIssueLineToken, @RequisitionIssueId, @RequisitionLineId, @QuantityIssued, @IssuedUnitId, @IssuedQuantity, @Notes, @CreatedBy);

    SELECT
        ril.RequisitionIssueLineId, ril.RequisitionIssueLineToken,
        ril.RequisitionIssueId, ri.RequisitionIssueToken,
        ril.RequisitionLineId, rl.RequisitionLineToken, rl.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        ril.QuantityIssued, ril.IssuedUnitId, iu.Code AS IssuedUnitCode, ril.IssuedQuantity,
        ril.Notes, ril.CreatedUtc, ril.CreatedBy
    FROM dbo.RequisitionIssueLines ril
    JOIN dbo.RequisitionIssues ri ON ri.RequisitionIssueId = ril.RequisitionIssueId
    JOIN dbo.RequisitionLines rl  ON rl.RequisitionLineId   = ril.RequisitionLineId
    JOIN dbo.Articles a           ON a.ArticleId             = rl.ArticleId
    LEFT JOIN dbo.UnitsOfMeasure iu ON iu.UnitOfMeasureId = ril.IssuedUnitId
    WHERE ril.RequisitionIssueLineToken = @RequisitionIssueLineToken;
END;
GO
