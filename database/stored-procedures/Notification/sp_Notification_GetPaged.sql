SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   NOTIFICATION - GET PAGED
   Always scoped to @UserId — the caller's own resolved EffectiveUserToken,
   resolved by NotificationService before calling this (no cross-user read
   path exists, unlike most GetPaged SPs elsewhere in this codebase which
   take an organization-hierarchy scope instead).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Notification_GetPaged
(
    @UserId       INT,
    @UnreadOnly   BIT = 0,
    @PageNumber   INT,
    @PageSize     INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        n.NotificationId, n.NotificationToken, n.UserId,
        nt.Code AS Type, n.DataJson, n.LinkUrl,
        n.IsRead, n.ReadUtc,
        n.CreatedUtc, n.CreatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.Notifications n
    JOIN dbo.NotificationTypes nt ON nt.NotificationTypeId = n.NotificationTypeId
    WHERE n.UserId = @UserId
      AND (@UnreadOnly = 0 OR n.IsRead = 0)
    ORDER BY n.CreatedUtc DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
