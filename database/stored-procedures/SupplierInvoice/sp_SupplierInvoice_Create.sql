SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERINVOICE - CREATE (header)
   Called once inside SupplierInvoiceService.CreateAsync's shared
   transaction, before the per-line and per-PO inserts. Assigns
   InternalSequentialNumber (FR-{Year}-{5-digit}) atomically from
   SupplierInvoiceNumberCounters — same UPDATE-first,
   INSERT-with-duplicate-key-retry concurrency-safe pattern as
   sp_PurchaseOrder_Create's own PurchaseOrderNumber assignment.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierInvoice_Create
(
    @SupplierInvoiceToken    UNIQUEIDENTIFIER,
    @OrganizationId          INT,
    @SupplierId              INT,
    @SupplierInvoiceNumber   VARCHAR(100),
    @InvoiceDate             DATE,
    @SupplierInvoiceStatusId INT,
    @Notes                   NVARCHAR(1000) = NULL,
    @CreatedBy               VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Year INT = YEAR(SYSUTCDATETIME());
    DECLARE @NextNumber INT;

    UPDATE dbo.SupplierInvoiceNumberCounters
        SET @NextNumber = LastNumber = LastNumber + 1
    WHERE OrganizationId = @OrganizationId AND Year = @Year;

    IF @@ROWCOUNT = 0
    BEGIN
        BEGIN TRY
            SET @NextNumber = 1;
            INSERT INTO dbo.SupplierInvoiceNumberCounters (OrganizationId, Year, LastNumber)
            VALUES (@OrganizationId, @Year, @NextNumber);
        END TRY
        BEGIN CATCH
            IF ERROR_NUMBER() IN (2601, 2627)
            BEGIN
                UPDATE dbo.SupplierInvoiceNumberCounters
                    SET @NextNumber = LastNumber = LastNumber + 1
                WHERE OrganizationId = @OrganizationId AND Year = @Year;
            END
            ELSE
                THROW;
        END CATCH
    END

    DECLARE @InternalSequentialNumber VARCHAR(20) = 'FR-' + CAST(@Year AS VARCHAR(4)) + '-' + RIGHT('00000' + CAST(@NextNumber AS VARCHAR(10)), 5);

    INSERT INTO dbo.SupplierInvoices
        (SupplierInvoiceToken, OrganizationId, SupplierId, SupplierInvoiceNumber, InternalSequentialNumber,
         InvoiceDate, SupplierInvoiceStatusId, Notes, CreatedBy)
    VALUES
        (@SupplierInvoiceToken, @OrganizationId, @SupplierId, @SupplierInvoiceNumber, @InternalSequentialNumber,
         @InvoiceDate, @SupplierInvoiceStatusId, @Notes, @CreatedBy);

    SELECT
        si.SupplierInvoiceId, si.SupplierInvoiceToken, si.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        si.SupplierId, s.SupplierToken, s.Name AS SupplierName,
        si.SupplierInvoiceNumber, si.InternalSequentialNumber, si.InvoiceDate,
        si.SupplierInvoiceStatusId, sis.Code AS Status,
        si.AttachmentUrl, si.Notes, si.CreatedUtc, si.CreatedBy
    FROM dbo.SupplierInvoices si
    JOIN dbo.Organizations org               ON org.OrganizationId = si.OrganizationId
    JOIN dbo.Suppliers s                     ON s.SupplierId       = si.SupplierId
    JOIN dbo.SupplierInvoiceStatuses sis      ON sis.SupplierInvoiceStatusId = si.SupplierInvoiceStatusId
    WHERE si.SupplierInvoiceToken = @SupplierInvoiceToken;
END;
GO
