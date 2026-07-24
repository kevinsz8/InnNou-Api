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
        tl.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        tl.Quantity, tl.Notes, tl.CreatedUtc, tl.CreatedBy
    FROM dbo.InventoryTransferLines tl
    JOIN dbo.Articles a ON a.ArticleId = tl.ArticleId
    WHERE tl.InventoryTransferId = @InventoryTransferId
    ORDER BY tl.InventoryTransferLineId;
END;
GO
