SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   NOTIFICATION - MARK AS READ
   @UserId in the WHERE clause is the real security boundary — without it a
   caller could mark any other user's notification read by guessing/enumerating
   tokens. NotificationService always passes the caller's own resolved UserId,
   never a caller-supplied one.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Notification_MarkAsRead
(
    @NotificationToken UNIQUEIDENTIFIER,
    @UserId              INT
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Notifications
    SET IsRead = 1,
        ReadUtc = SYSUTCDATETIME()
    WHERE NotificationToken = @NotificationToken
      AND UserId = @UserId
      AND IsRead = 0;
END;
GO
