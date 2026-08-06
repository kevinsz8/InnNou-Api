SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- Reverse lookup of SupplierInvoiceGoodsReceipts' own UNIQUE(GoodsReceiptId) — given a set of
-- GoodsReceiptIds, which SupplierInvoice(s) (if any) already cover them. Feeds Nota de Crédito's
-- own auto-detection of which Facturas it corrects — see
-- migrations/20260807_SupplierCreditNotes_Create.sql's own header comment for why this is never
-- user-picked.
CREATE OR ALTER PROCEDURE dbo.sp_SupplierInvoiceGoodsReceipt_GetInvoiceIdsByGoodsReceiptIds
(
    @GoodsReceiptIds VARCHAR(MAX)
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT sigr.SupplierInvoiceId
    FROM dbo.SupplierInvoiceGoodsReceipts sigr
    JOIN (SELECT CAST(value AS INT) AS GoodsReceiptId FROM STRING_SPLIT(@GoodsReceiptIds, ',')) t
      ON t.GoodsReceiptId = sigr.GoodsReceiptId;
END;
GO
