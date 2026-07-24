SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INVENTORYTRANSFERLINE - CREATE
   Single-line insert + re-select, called once per line in a C# loop inside
   InventoryService.CreateTransferAsync's shared transaction — same
   one-call-per-line shape as sp_GoodsReceiptLine_Create/
   sp_PurchaseOrderLineRectification_Create.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InventoryTransferLine_Create
(
    @InventoryTransferLineToken UNIQUEIDENTIFIER,
    @InventoryTransferId        INT,
    @ArticleId                  INT,
    @Quantity                   DECIMAL(18,4),
    @Notes                      NVARCHAR(500) = NULL,
    @CreatedBy                  VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.InventoryTransferLines (InventoryTransferLineToken, InventoryTransferId, ArticleId, Quantity, Notes, CreatedBy)
    VALUES (@InventoryTransferLineToken, @InventoryTransferId, @ArticleId, @Quantity, @Notes, @CreatedBy);

    SELECT
        tl.InventoryTransferLineId, tl.InventoryTransferLineToken, tl.InventoryTransferId,
        tl.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        tl.Quantity, tl.Notes, tl.CreatedUtc, tl.CreatedBy
    FROM dbo.InventoryTransferLines tl
    JOIN dbo.Articles a ON a.ArticleId = tl.ArticleId
    WHERE tl.InventoryTransferLineToken = @InventoryTransferLineToken;
END;
GO
