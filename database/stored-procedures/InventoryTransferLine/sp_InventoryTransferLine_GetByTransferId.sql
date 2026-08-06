SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INVENTORYTRANSFERLINE - GET BY TRANSFER ID
   Lines for a single InventoryTransfer — populates InventoryTransferDto.Lines.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InventoryTransferLine_GetByTransferId
(
    @InventoryTransferId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        tl.InventoryTransferLineId, tl.InventoryTransferLineToken, tl.InventoryTransferId,
        tl.ArticleId, a.ArticleToken, a.Name AS ArticleName, a.PurchaseUnitId, u.Code AS PurchaseUnitCode,
        tl.Quantity, tl.TransferredUnitId, tu.Code AS TransferredUnitCode, tl.TransferredQuantity,
        tl.Notes, tl.CreatedUtc, tl.CreatedBy
    FROM dbo.InventoryTransferLines tl
    JOIN dbo.Articles a ON a.ArticleId = tl.ArticleId
    JOIN dbo.UnitsOfMeasure u ON u.UnitOfMeasureId = a.PurchaseUnitId
    LEFT JOIN dbo.UnitsOfMeasure tu ON tu.UnitOfMeasureId = tl.TransferredUnitId
    WHERE tl.InventoryTransferId = @InventoryTransferId
    ORDER BY tl.InventoryTransferLineId;
END;
GO
