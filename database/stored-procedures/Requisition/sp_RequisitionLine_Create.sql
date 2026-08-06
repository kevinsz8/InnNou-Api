SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   REQUISITIONLINE - CREATE
   Only reachable while the parent Requisition is REQUESTED (enforced in
   the service, not here — same "service checks status, SP just inserts"
   split as OrderLine's own AddLineAsync).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_RequisitionLine_Create
(
    @RequisitionLineToken UNIQUEIDENTIFIER,
    @RequisitionId          INT,
    @ArticleId               INT,
    @QuantityRequested        DECIMAL(18,8),
    @RequestedUnitId          INT = NULL,
    @RequestedQuantity        DECIMAL(18,8) = NULL,
    @Notes                    NVARCHAR(500) = NULL,
    @CreatedBy                VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.RequisitionLines (RequisitionLineToken, RequisitionId, ArticleId, QuantityRequested, RequestedUnitId, RequestedQuantity, Notes, CreatedBy)
    VALUES (@RequisitionLineToken, @RequisitionId, @ArticleId, @QuantityRequested, @RequestedUnitId, @RequestedQuantity, @Notes, @CreatedBy);

    SELECT
        rl.RequisitionLineId, rl.RequisitionLineToken, rl.RequisitionId, r.RequisitionToken,
        rl.ArticleId, a.ArticleToken, a.Name AS ArticleName, a.PurchaseUnitId, u.Code AS PurchaseUnitCode,
        rl.QuantityRequested, rl.RequestedUnitId, ru.Code AS RequestedUnitCode, rl.RequestedQuantity,
        rl.Notes, rl.CreatedUtc, rl.CreatedBy
    FROM dbo.RequisitionLines rl
    JOIN dbo.Requisitions r ON r.RequisitionId = rl.RequisitionId
    JOIN dbo.Articles a ON a.ArticleId = rl.ArticleId
    JOIN dbo.UnitsOfMeasure u ON u.UnitOfMeasureId = a.PurchaseUnitId
    LEFT JOIN dbo.UnitsOfMeasure ru ON ru.UnitOfMeasureId = rl.RequestedUnitId
    WHERE rl.RequisitionLineToken = @RequisitionLineToken;
END;
GO
