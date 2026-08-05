SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   REQUISITIONLINE - DELETE
   Hard delete — only reachable while the parent Requisition is still
   REQUESTED (enforced in the service), so no issuance can ever reference
   this line yet.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_RequisitionLine_Delete
(
    @RequisitionLineToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.RequisitionLines WHERE RequisitionLineToken = @RequisitionLineToken;
END;
GO
