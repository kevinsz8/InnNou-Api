SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PARLEVELOVERRIDE - DELETE
   Hard delete — same reasoning as sp_ParLevel_Delete (operational
   configuration, not an audit/financial record).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_ParLevelOverride_Delete
(
    @ParLevelOverrideToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.ParLevelOverrides WHERE ParLevelOverrideToken = @ParLevelOverrideToken;

    SELECT CAST(@@ROWCOUNT AS BIT) AS Deleted;
END;
GO
