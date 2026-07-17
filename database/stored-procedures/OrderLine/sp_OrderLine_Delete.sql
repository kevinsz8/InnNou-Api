SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDERLINE - DELETE
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_OrderLine_Delete
(
    @OrderLineToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.OrderLine WHERE OrderLineToken = @OrderLineToken;
END;
GO
