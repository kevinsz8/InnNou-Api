SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERCREDITNOTE - CREATE
   Header-only insert — lines/tax-breakdown/invoice-links are inserted
   separately by the caller inside the same transaction (same shape as
   sp_SupplierInvoice_Create + sp_SupplierInvoiceLine_Create). Internal
   sequential number assignment uses the same UPDATE-first,
   INSERT-with-duplicate-key-retry concurrency-safe pattern as
   sp_SupplierInvoice_Create's own SupplierInvoiceNumberCounters.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierCreditNote_Create
(
    @SupplierCreditNoteToken UNIQUEIDENTIFIER,
    @SupplierReturnId        INT,
    @OrganizationId          INT,
    @SupplierId              INT,
    @CreditNoteNumber        VARCHAR(100),
    @CreditNoteDate          DATE,
    @Reason                  NVARCHAR(500),
    @Notes                   NVARCHAR(1000) = NULL,
    @CreatedBy               VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Year INT = YEAR(SYSUTCDATETIME());
    DECLARE @NextNumber INT;

    UPDATE dbo.SupplierCreditNoteNumberCounters
        SET @NextNumber = LastNumber = LastNumber + 1
    WHERE OrganizationId = @OrganizationId AND Year = @Year;

    IF @@ROWCOUNT = 0
    BEGIN
        BEGIN TRY
            SET @NextNumber = 1;
            INSERT INTO dbo.SupplierCreditNoteNumberCounters (OrganizationId, Year, LastNumber)
            VALUES (@OrganizationId, @Year, @NextNumber);
        END TRY
        BEGIN CATCH
            IF ERROR_NUMBER() IN (2601, 2627)
            BEGIN
                UPDATE dbo.SupplierCreditNoteNumberCounters
                    SET @NextNumber = LastNumber = LastNumber + 1
                WHERE OrganizationId = @OrganizationId AND Year = @Year;
            END
            ELSE
                THROW;
        END CATCH
    END

    DECLARE @InternalSequentialNumber VARCHAR(20) = 'NC-' + CAST(@Year AS VARCHAR(4)) + '-' + RIGHT('00000' + CAST(@NextNumber AS VARCHAR(10)), 5);

    INSERT INTO dbo.SupplierCreditNotes
        (SupplierCreditNoteToken, SupplierReturnId, OrganizationId, SupplierId,
         CreditNoteNumber, InternalSequentialNumber, CreditNoteDate, Reason, Notes, CreatedBy)
    VALUES
        (@SupplierCreditNoteToken, @SupplierReturnId, @OrganizationId, @SupplierId,
         @CreditNoteNumber, @InternalSequentialNumber, @CreditNoteDate, @Reason, @Notes, @CreatedBy);

    SELECT
        scn.SupplierCreditNoteId, scn.SupplierCreditNoteToken,
        scn.SupplierReturnId, sr.SupplierReturnToken, sr.PurchaseOrderId, po.PurchaseOrderToken, po.PurchaseOrderNumber, po.WarehouseId,
        scn.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        scn.SupplierId, s.SupplierToken, s.Name AS SupplierName,
        scn.CreditNoteNumber, scn.InternalSequentialNumber, scn.CreditNoteDate, scn.Reason, scn.Notes,
        scn.CreatedUtc, scn.CreatedBy
    FROM dbo.SupplierCreditNotes scn
    JOIN dbo.SupplierReturns sr ON sr.SupplierReturnId = scn.SupplierReturnId
    JOIN dbo.PurchaseOrder po   ON po.PurchaseOrderId  = sr.PurchaseOrderId
    JOIN dbo.Organizations org  ON org.OrganizationId  = scn.OrganizationId
    JOIN dbo.Suppliers s        ON s.SupplierId        = scn.SupplierId
    WHERE scn.SupplierCreditNoteToken = @SupplierCreditNoteToken;
END;
GO
