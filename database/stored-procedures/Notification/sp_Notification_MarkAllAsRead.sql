SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   NOTIFICATION - MARK ALL AS READ
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Notification_MarkAllAsRead
(
    @UserId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Notifications
    SET IsRead = 1,
        ReadUtc = SYSUTCDATETIME()
    WHERE UserId = @UserId
      AND IsRead = 0;
END;
GO
