SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INVENTORYTRANSFER - GET BY TOKEN
   Header only — sp_InventoryTransferLine_GetByTransferId populates Lines,
   same "second query, always" convention as GoodsReceiptDto.Lines.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InventoryTransfer_GetByToken
(
    @InventoryTransferToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        it.InventoryTransferId, it.InventoryTransferToken,
        it.FromWarehouseId, fw.WarehouseToken AS FromWarehouseToken, fw.Name AS FromWarehouseName, fw.OrganizationId AS FromOrganizationId,
        it.ToWarehouseId, tw.WarehouseToken AS ToWarehouseToken, tw.Name AS ToWarehouseName, tw.OrganizationId AS ToOrganizationId,
        it.Notes, it.CreatedUtc, it.CreatedBy
    FROM dbo.InventoryTransfers it
    JOIN dbo.Warehouses fw ON fw.WarehouseId = it.FromWarehouseId
    JOIN dbo.Warehouses tw ON tw.WarehouseId = it.ToWarehouseId
    WHERE it.InventoryTransferToken = @InventoryTransferToken;
END;
GO
