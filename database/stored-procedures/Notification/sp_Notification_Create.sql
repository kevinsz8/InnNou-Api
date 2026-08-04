SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   NOTIFICATION - CREATE
   Called from NotificationService.NotifyAsync, itself called best-effort/
   non-blocking from the triggering service (same try/catch convention as
   the existing email call sites in OrderService — a failure here must never
   fail the action that triggered it). The C# layer pushes the same payload
   over the SignalR hub right after this insert succeeds.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Notification_Create
(
    @NotificationToken UNIQUEIDENTIFIER,
    @UserId             INT,
    @Type                VARCHAR(40),
    @DataJson            NVARCHAR(1000),
    @LinkUrl             NVARCHAR(500) = NULL,
    @CreatedBy           VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Notifications
        (NotificationToken, UserId, NotificationTypeId, DataJson, LinkUrl, CreatedBy)
    VALUES
        (@NotificationToken, @UserId,
         (SELECT NotificationTypeId FROM dbo.NotificationTypes WHERE Code = @Type),
         @DataJson, @LinkUrl, @CreatedBy);

    SELECT
        n.NotificationId, n.NotificationToken, n.UserId,
        nt.Code AS Type, n.DataJson, n.LinkUrl,
        n.IsRead, n.ReadUtc,
        n.CreatedUtc, n.CreatedBy
    FROM dbo.Notifications n
    JOIN dbo.NotificationTypes nt ON nt.NotificationTypeId = n.NotificationTypeId
    WHERE n.NotificationToken = @NotificationToken;
END;
GO
