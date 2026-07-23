SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_ConsolidatedPurchaseOrderMember_Create
(
    @ConsolidatedPurchaseOrderId INT,
    @PurchaseOrderId             INT,
    @CreatedBy                   VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.ConsolidatedPurchaseOrderMembers (ConsolidatedPurchaseOrderId, PurchaseOrderId, CreatedBy)
    VALUES (@ConsolidatedPurchaseOrderId, @PurchaseOrderId, @CreatedBy);
END;
GO
