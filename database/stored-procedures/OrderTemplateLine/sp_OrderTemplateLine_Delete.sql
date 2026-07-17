SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDERTEMPLATELINE - DELETE
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_OrderTemplateLine_Delete
(
    @OrderTemplateLineToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.OrderTemplateLine WHERE OrderTemplateLineToken = @OrderTemplateLineToken;
END;
GO
