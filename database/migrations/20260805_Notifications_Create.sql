-- =============================================================
-- MIGRATION: Create Notifications module (in-app, real-time via SignalR)
-- Date: 2026-08-05
-- =============================================================
-- The bell icon in InnNou-Web's DashboardLayout has existed since early on but was purely
-- decorative (no data behind it) — this wires it up. Deliberately NOT resolved to display text
-- server-side: DataJson carries only the raw interpolation parameters (e.g.
-- {"orderNumber":"PO-2026-00042"}), and the frontend renders the actual sentence via i18next —
-- same "never fix one language server-side" philosophy CatalogTranslations/GoodsReceipt tax
-- preview already follow. NotificationTypeId is Id-backed per this codebase's standing rule
-- (CLAUDE.md "Status/type fields are Id-backed" — a past project was burned by corrupted
-- text-status data).
--
-- Delivery is push-first (SignalR hub, one group per resolved UserId, joined/left on
-- connect/disconnect) — no polling. The table itself is still the source of truth (GetPaged/
-- GetUnreadCount/MarkAsRead all read straight from it), so a client that was offline when a
-- push fired still sees it on next login/reconnect.
--
-- Idempotent — safe to re-run.
-- =============================================================

-- ── NotificationTypes (lookup) ──────────────────────────────────────────
IF OBJECT_ID('NotificationTypes', 'U') IS NULL
BEGIN
    CREATE TABLE NotificationTypes (
        NotificationTypeId int         NOT NULL IDENTITY(1,1),
        Code               varchar(40) NOT NULL,
        IsActive           bit         NOT NULL DEFAULT 1,

        CONSTRAINT PK_NotificationTypes PRIMARY KEY (NotificationTypeId),
        CONSTRAINT UQ_NotificationTypes_Code UNIQUE (Code)
    );
END
GO

-- Seed order matters — the C# NotificationType enum hardcodes these Ids.
IF NOT EXISTS (SELECT 1 FROM NotificationTypes WHERE Code = 'ORDER_CONFIRMED')
    INSERT INTO NotificationTypes (Code) VALUES ('ORDER_CONFIRMED');
GO
IF NOT EXISTS (SELECT 1 FROM NotificationTypes WHERE Code = 'NEW_PURCHASE_ORDER')
    INSERT INTO NotificationTypes (Code) VALUES ('NEW_PURCHASE_ORDER');
GO
IF NOT EXISTS (SELECT 1 FROM NotificationTypes WHERE Code = 'APPROVAL_REQUESTED')
    INSERT INTO NotificationTypes (Code) VALUES ('APPROVAL_REQUESTED');
GO
IF NOT EXISTS (SELECT 1 FROM NotificationTypes WHERE Code = 'APPROVAL_STEP_APPROVED')
    INSERT INTO NotificationTypes (Code) VALUES ('APPROVAL_STEP_APPROVED');
GO
IF NOT EXISTS (SELECT 1 FROM NotificationTypes WHERE Code = 'APPROVAL_STEP_REJECTED')
    INSERT INTO NotificationTypes (Code) VALUES ('APPROVAL_STEP_REJECTED');
GO

-- ── Notifications (append-only except IsRead/ReadUtc) ───────────────────
IF OBJECT_ID('Notifications', 'U') IS NULL
BEGIN
    CREATE TABLE Notifications (
        NotificationId     int              NOT NULL IDENTITY(1,1),
        NotificationToken  uniqueidentifier NOT NULL DEFAULT NEWID(),
        UserId              int              NOT NULL,   -- target recipient (Users.UserId)
        NotificationTypeId  int              NOT NULL,

        -- Raw interpolation params for the frontend's i18next template — never a fixed-language
        -- rendered sentence. e.g. {"orderNumber":"PO-2026-00042","organizationName":"..."}.
        DataJson            nvarchar(1000)   NOT NULL,
        LinkUrl              nvarchar(500)        NULL,   -- frontend route to open on click, e.g. /orders/{token}

        IsRead               bit              NOT NULL DEFAULT (0),
        ReadUtc              datetime2            NULL,

        CreatedUtc           datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy            varchar(150)         NULL,   -- the actor whose action triggered this notification

        CONSTRAINT PK_Notifications PRIMARY KEY (NotificationId),
        CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES Users (UserId),
        CONSTRAINT FK_Notifications_NotificationTypes FOREIGN KEY (NotificationTypeId) REFERENCES NotificationTypes (NotificationTypeId),
        CONSTRAINT CK_Notifications_DataJsonIsJson CHECK (ISJSON(DataJson) = 1)
    );

    CREATE UNIQUE INDEX UQ_Notifications_NotificationToken ON Notifications (NotificationToken);
    -- Backs both "my unread count" and "my recent notifications, newest first" — the two hot
    -- read paths (polled... no, pushed, but still queried on initial load/reconnect).
    CREATE INDEX IX_Notifications_UserId_IsRead_CreatedUtc ON Notifications (UserId, IsRead, CreatedUtc DESC);
END
GO

PRINT '=== Migration 20260805_Notifications_Create completed successfully ===';
GO
