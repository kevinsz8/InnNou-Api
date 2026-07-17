SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDERTEMPLATE - DELETE
   Hard delete — a personal scratch list has no audit-trail need,
   matching ArticleFavorites' shape rather than Warehouse's soft delete.
   OrderTemplateLine rows cascade via FK_OrderTemplateLine_OrderTemplate
   ON DELETE CASCADE.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_OrderTemplate_Delete
(
    @OrderTemplateToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.OrderTemplate WHERE OrderTemplateToken = @OrderTemplateToken;
END;
GO
