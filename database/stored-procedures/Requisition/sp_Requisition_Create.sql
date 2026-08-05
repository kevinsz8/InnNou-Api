SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   REQUISITION - CREATE
   Header only — lines are inserted separately via sp_RequisitionLine_Create,
   called in a loop from the same C# transaction (mirrors PurchaseOrder/
   InternalOrder's own two-SP relationship).

   RequisitionNumber (REQ-{Year}-{5-digit number}) is assigned atomically
   from RequisitionNumberCounters, scoped per OrganizationId per calendar
   year — same UPDATE-first, INSERT-with-duplicate-key-retry shape as
   sp_PurchaseOrder_Create/sp_InternalOrder_Create's own counter logic.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Requisition_Create
(
    @RequisitionToken UNIQUEIDENTIFIER,
    @OrganizationId   INT,
    @WarehouseId      INT,
    @DepartmentId     INT,
    @Notes            NVARCHAR(1000) = NULL,
    @CreatedBy        VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Year INT = YEAR(SYSUTCDATETIME());
    DECLARE @NextNumber INT;

    UPDATE dbo.RequisitionNumberCounters
        SET @NextNumber = LastNumber = LastNumber + 1
    WHERE OrganizationId = @OrganizationId AND Year = @Year;

    IF @@ROWCOUNT = 0
    BEGIN
        BEGIN TRY
            SET @NextNumber = 1;
            INSERT INTO dbo.RequisitionNumberCounters (OrganizationId, Year, LastNumber)
            VALUES (@OrganizationId, @Year, @NextNumber);
        END TRY
        BEGIN CATCH
            IF ERROR_NUMBER() IN (2601, 2627)
            BEGIN
                UPDATE dbo.RequisitionNumberCounters
                    SET @NextNumber = LastNumber = LastNumber + 1
                WHERE OrganizationId = @OrganizationId AND Year = @Year;
            END
            ELSE
                THROW;
        END CATCH
    END

    DECLARE @RequisitionNumber VARCHAR(20) = 'REQ-' + CAST(@Year AS VARCHAR(4)) + '-' + RIGHT('00000' + CAST(@NextNumber AS VARCHAR(10)), 5);

    INSERT INTO dbo.Requisitions
        (RequisitionToken, RequisitionNumber, OrganizationId, WarehouseId, DepartmentId, Notes, CreatedBy)
    VALUES
        (@RequisitionToken, @RequisitionNumber, @OrganizationId, @WarehouseId, @DepartmentId, @Notes, @CreatedBy);

    SELECT
        r.RequisitionId, r.RequisitionToken, r.RequisitionNumber,
        r.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        r.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        r.DepartmentId, d.DepartmentToken, d.Name AS DepartmentName,
        rs.Code AS Status,
        r.Notes,
        r.ApprovedUtc, r.ApprovedBy,
        r.RejectedUtc, r.RejectedBy, r.RejectedReason,
        r.CancelledUtc, r.CancelledBy, r.CancelledReason,
        r.ClosedShortUtc, r.ClosedShortBy, r.ClosedShortReason,
        r.CreatedUtc, r.CreatedBy, r.LastUpdatedUtc, r.LastUpdatedBy
    FROM dbo.Requisitions r
    JOIN dbo.Organizations org ON org.OrganizationId = r.OrganizationId
    JOIN dbo.Warehouses w      ON w.WarehouseId      = r.WarehouseId
    JOIN dbo.Departments d     ON d.DepartmentId     = r.DepartmentId
    JOIN dbo.RequisitionStatuses rs ON rs.RequisitionStatusId = r.RequisitionStatusId
    WHERE r.RequisitionToken = @RequisitionToken;
END;
GO
