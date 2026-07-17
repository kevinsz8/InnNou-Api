-- =============================================================
-- MIGRATION: Create Order Templates module tables (OrderTemplate, OrderTemplateLine)
-- Date: 2026-07-18
-- =============================================================
-- A reusable, named shopping list of (Article, Quantity) pairs a user can
-- apply to a Draft Order in one action, or export/re-import as Excel.
-- Deliberately NOT a snapshot like Order/OrderLine — price is never stored
-- here, it is always resolved fresh at apply-time via the same
-- OrderService.AddLineAsync path a manual catalog add already uses.
--
-- Scope is Organization + Warehouse + Owner(User) — personal to the
-- creating user, tagged to one org+warehouse. A Super Asociado-org caller
-- is the one exception: they may see/edit every descendant Asociado's
-- templates regardless of Warehouse/Owner (see OrderTemplateService's
-- CanAccessTemplateAsync) — Super Asociado is fully write-blocked for
-- Orders themselves, but not for Templates, since a template is just a
-- reusable list, never itself a purchase.
--
-- Creation order matters: OrderTemplate -> OrderTemplateLine (FK to
-- OrderTemplate, ON DELETE CASCADE — a hard delete of a template always
-- removes its lines, mirroring ArticleFavorites' hard-delete shape rather
-- than Warehouse's soft delete; a personal scratch list has no audit-trail
-- need).
--
-- Guarded so each CREATE TABLE is a no-op if already applied.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('OrderTemplate', 'U') IS NULL
BEGIN
    CREATE TABLE OrderTemplate
    (
        OrderTemplateId    int              NOT NULL IDENTITY(1,1),
        OrderTemplateToken uniqueidentifier NOT NULL DEFAULT NEWID(),
        Name               nvarchar(200)    NOT NULL,
        OrganizationId     int              NOT NULL,   -- denormalized from Warehouse at creation, like Order
        WarehouseId        int              NOT NULL,
        OwnerUserId        int              NOT NULL,   -- resolved from context.EffectiveUserToken at creation, immutable

        CreatedUtc         datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy          varchar(150)     NOT NULL,   -- ActorUserToken (real actor), per impersonation convention
        LastUpdatedUtc     datetime2            NULL,
        LastUpdatedBy      varchar(150)         NULL,

        CONSTRAINT PK_OrderTemplate PRIMARY KEY (OrderTemplateId),
        CONSTRAINT FK_OrderTemplate_Organizations FOREIGN KEY (OrganizationId) REFERENCES Organizations (OrganizationId),
        CONSTRAINT FK_OrderTemplate_Warehouses FOREIGN KEY (WarehouseId) REFERENCES Warehouses (WarehouseId),
        CONSTRAINT FK_OrderTemplate_Users FOREIGN KEY (OwnerUserId) REFERENCES Users (UserId)
    );

    CREATE UNIQUE INDEX UQ_OrderTemplate_OrderTemplateToken ON OrderTemplate (OrderTemplateToken);
    CREATE        INDEX IX_OrderTemplate_OrganizationId    ON OrderTemplate (OrganizationId);
    CREATE        INDEX IX_OrderTemplate_WarehouseId       ON OrderTemplate (WarehouseId);
    CREATE        INDEX IX_OrderTemplate_OwnerUserId       ON OrderTemplate (OwnerUserId);
END
GO

IF OBJECT_ID('OrderTemplateLine', 'U') IS NULL
BEGIN
    CREATE TABLE OrderTemplateLine
    (
        OrderTemplateLineId    int              NOT NULL IDENTITY(1,1),
        OrderTemplateLineToken uniqueidentifier NOT NULL DEFAULT NEWID(),
        OrderTemplateId        int              NOT NULL,
        ArticleId              int              NOT NULL,     -- traceability only, price NEVER stored

        Quantity               decimal(18,4)    NOT NULL,

        CreatedUtc             datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy              varchar(150)     NOT NULL,
        LastUpdatedUtc         datetime2            NULL,
        LastUpdatedBy          varchar(150)         NULL,

        CONSTRAINT PK_OrderTemplateLine PRIMARY KEY (OrderTemplateLineId),
        CONSTRAINT FK_OrderTemplateLine_OrderTemplate FOREIGN KEY (OrderTemplateId)
            REFERENCES OrderTemplate (OrderTemplateId) ON DELETE CASCADE,
        CONSTRAINT FK_OrderTemplateLine_Articles FOREIGN KEY (ArticleId) REFERENCES Articles (ArticleId)
    );

    CREATE UNIQUE INDEX UQ_OrderTemplateLine_OrderTemplateLineToken ON OrderTemplateLine (OrderTemplateLineToken);

    -- One line per Article per Template — the upsert target for sp_OrderTemplateLine_Upsert,
    -- same shape as UX_OrderLine_Order_Article.
    CREATE UNIQUE INDEX UX_OrderTemplateLine_Template_Article ON OrderTemplateLine (OrderTemplateId, ArticleId);

    CREATE INDEX IX_OrderTemplateLine_ArticleId ON OrderTemplateLine (ArticleId);
END
GO
