SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   NOTIFICATION - GET UNREAD COUNT
   Backs the bell icon's badge on initial page load (the SignalR hub keeps
   it live after that — this is only the cold-start/reconnect value).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Notification_GetUnreadCount
(
    @UserId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS UnreadCount
    FROM dbo.Notifications
    WHERE UserId = @UserId AND IsRead = 0;
END;
GO
