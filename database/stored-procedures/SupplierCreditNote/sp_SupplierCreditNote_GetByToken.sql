SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_SupplierCreditNote_GetByToken
(
    @SupplierCreditNoteToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        scn.SupplierCreditNoteId, scn.SupplierCreditNoteToken,
        scn.SupplierReturnId, sr.SupplierReturnToken, sr.PurchaseOrderId, po.PurchaseOrderToken, po.PurchaseOrderNumber, po.WarehouseId,
        scn.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        scn.SupplierId, s.SupplierToken, s.Name AS SupplierName,
        scn.CreditNoteNumber, scn.InternalSequentialNumber, scn.CreditNoteDate, scn.Reason, scn.Notes,
        scn.CreatedUtc, scn.CreatedBy
    FROM dbo.SupplierCreditNotes scn
    JOIN dbo.SupplierReturns sr  ON sr.SupplierReturnId = scn.SupplierReturnId
    JOIN dbo.PurchaseOrder po    ON po.PurchaseOrderId  = sr.PurchaseOrderId
    JOIN dbo.Organizations org   ON org.OrganizationId  = scn.OrganizationId
    JOIN dbo.Suppliers s         ON s.SupplierId        = scn.SupplierId
    WHERE scn.SupplierCreditNoteToken = @SupplierCreditNoteToken;
END;
GO
