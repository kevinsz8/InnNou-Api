-- ================================================================
-- InnNou — Database Schema Snapshot
-- Date        : 2026-06-27
-- Description : State after Articles Module V1 schema design.
--               Covers catalog tables (UnitTypes, UnitsOfMeasure,
--               UnitConversionRates, Families, SubFamilies,
--               Categories, SubCategories), Articles, ArticlePrices,
--               HotelArticles, and all supporting tables.
--
-- Includes    : Tables, PKs, FKs, unique constraints, indexes.
-- Excludes    : Stored procedures (kept in database/stored-procedures/).
--
-- Usage:
--   Run against a blank SQL Server instance to recreate the schema.
--   To reset an existing InnNou DB to this state:
--       DROP DATABASE InnNou;
--   then re-run this script.
-- ================================================================

USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'InnNou')
    CREATE DATABASE InnNou;
GO

USE InnNou;
GO

-- ================================================================
-- TABLES  — created in FK dependency order
-- ================================================================

-- ----------------------------------------------------------------
-- Roles
-- ----------------------------------------------------------------
CREATE TABLE Roles (
    RoleId          int              NOT NULL IDENTITY(1,1),
    RoleToken       uniqueidentifier NOT NULL DEFAULT NEWID(),
    Name            varchar(100)     NOT NULL,
    NormalizedName  varchar(100)     NOT NULL,
    Description     varchar(300)         NULL,
    RoleLevel       int              NOT NULL,
    CanImpersonate  bit              NOT NULL DEFAULT (0),
    IsActive        bit              NOT NULL DEFAULT (1),
    CreatedUtc      datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy       varchar(150)         NULL,
    LastUpdatedUtc  datetime2            NULL,
    LastUpdatedBy   varchar(150)         NULL,

    CONSTRAINT PK_Roles PRIMARY KEY (RoleId)
);
CREATE UNIQUE INDEX UQ_Roles_RoleToken      ON Roles (RoleToken);
CREATE UNIQUE INDEX UQ_Roles_NormalizedName ON Roles (NormalizedName);
GO

-- ----------------------------------------------------------------
-- Hotels  (self-referencing: ParentHotelId)
-- ----------------------------------------------------------------
CREATE TABLE Hotels (
    HotelId         int              NOT NULL IDENTITY(1,1),
    HotelToken      uniqueidentifier NOT NULL DEFAULT NEWID(),
    Name            varchar(200)     NOT NULL,
    NormalizedName  varchar(200)     NOT NULL,
    LegalName       varchar(250)         NULL,
    Code            varchar(50)          NULL,
    ParentHotelId   int                  NULL,
    TimeZone        varchar(100)         NULL,
    CurrencyCode    varchar(10)          NULL,
    LanguageCode    varchar(10)          NULL,
    IsActive        bit              NOT NULL DEFAULT (1),
    IsDeleted       bit              NOT NULL DEFAULT (0),
    CreatedUtc      datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy       varchar(150)         NULL,
    LastUpdatedUtc  datetime2            NULL,
    LastUpdatedBy   varchar(150)         NULL,
    DeletedUtc      datetime2            NULL,
    DeletedBy       varchar(150)         NULL,

    CONSTRAINT PK_Hotels            PRIMARY KEY (HotelId),
    CONSTRAINT FK_Hotels_ParentHotel FOREIGN KEY (ParentHotelId) REFERENCES Hotels (HotelId)
);
CREATE UNIQUE INDEX UQ_Hotels_HotelToken                 ON Hotels (HotelToken);
CREATE        INDEX IX_Hotels_ParentHotelId              ON Hotels (ParentHotelId);
CREATE        INDEX UX_Hotels_NormalizedName_NotDeleted  ON Hotels (NormalizedName);
GO

-- ----------------------------------------------------------------
-- Suppliers
-- ----------------------------------------------------------------
CREATE TABLE Suppliers (
    SupplierId      int              NOT NULL IDENTITY(1,1),
    SupplierToken   uniqueidentifier NOT NULL DEFAULT NEWID(),
    Name            varchar(200)     NOT NULL,
    NormalizedName  varchar(200)     NOT NULL,
    LegalName       varchar(250)         NULL,
    TaxId           varchar(50)          NULL,
    Email           varchar(320)         NULL,
    Phone           varchar(50)          NULL,
    AddressLine1    varchar(250)         NULL,
    AddressLine2    varchar(250)         NULL,
    City            varchar(150)         NULL,
    State           varchar(150)         NULL,
    PostalCode      varchar(50)          NULL,
    Country         varchar(100)         NULL,
    IsGlobal        bit              NOT NULL DEFAULT (1),
    IsActive        bit              NOT NULL DEFAULT (1),
    IsDeleted       bit              NOT NULL DEFAULT (0),
    CreatedUtc      datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy       varchar(150)         NULL,
    LastUpdatedUtc  datetime2            NULL,
    LastUpdatedBy   varchar(150)         NULL,
    DeletedUtc      datetime2            NULL,
    DeletedBy       varchar(150)         NULL,

    CONSTRAINT PK_Suppliers PRIMARY KEY (SupplierId)
);
CREATE UNIQUE INDEX UQ_Suppliers_SupplierToken             ON Suppliers (SupplierToken);
CREATE        INDEX UX_Suppliers_NormalizedName_NotDeleted ON Suppliers (NormalizedName);
GO

-- ----------------------------------------------------------------
-- UnitTypes  (catalog — system-owned)
-- ----------------------------------------------------------------
CREATE TABLE UnitTypes (
    UnitTypeId      int              NOT NULL IDENTITY(1,1),
    UnitTypeToken   uniqueidentifier NOT NULL DEFAULT NEWID(),
    Code            varchar(50)      NOT NULL,
    IsSystem        bit              NOT NULL DEFAULT (1),
    IsActive        bit              NOT NULL DEFAULT (1),
    CreatedUtc      datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy       varchar(150)         NULL,
    LastUpdatedUtc  datetime2            NULL,
    LastUpdatedBy   nvarchar(150)        NULL,

    CONSTRAINT PK_UnitTypes PRIMARY KEY (UnitTypeId)
);
CREATE UNIQUE INDEX UQ_UnitTypes_Code ON UnitTypes (Code);
GO

-- ----------------------------------------------------------------
-- Families  (catalog — system-owned)
-- ----------------------------------------------------------------
CREATE TABLE Families (
    FamilyId        int              NOT NULL IDENTITY(1,1),
    FamilyToken     uniqueidentifier NOT NULL DEFAULT NEWID(),
    Code            varchar(100)     NOT NULL,
    IsSystem        bit              NOT NULL DEFAULT (1),
    IsActive        bit              NOT NULL DEFAULT (1),
    CreatedUtc      datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy       varchar(150)         NULL,
    LastUpdatedUtc  datetime2            NULL,
    LastUpdatedBy   nvarchar(150)        NULL,

    CONSTRAINT PK_Families PRIMARY KEY (FamilyId)
);
CREATE UNIQUE INDEX UQ_Families_Code ON Families (Code);
GO

-- ----------------------------------------------------------------
-- Categories  (catalog — system-owned)
-- ----------------------------------------------------------------
CREATE TABLE Categories (
    CategoryId      int              NOT NULL IDENTITY(1,1),
    CategoryToken   uniqueidentifier NOT NULL DEFAULT NEWID(),
    Code            varchar(100)     NOT NULL,
    IsSystem        bit              NOT NULL DEFAULT (1),
    IsActive        bit              NOT NULL DEFAULT (1),
    CreatedUtc      datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy       varchar(150)         NULL,
    LastUpdatedUtc  datetime2            NULL,
    LastUpdatedBy   nvarchar(150)        NULL,

    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId)
);
CREATE UNIQUE INDEX UQ_Categories_Code ON Categories (Code);
GO

-- ----------------------------------------------------------------
-- UnitsOfMeasure  (catalog — depends on UnitTypes)
-- ----------------------------------------------------------------
CREATE TABLE UnitsOfMeasure (
    UnitOfMeasureId     int              NOT NULL IDENTITY(1,1),
    UnitOfMeasureToken  uniqueidentifier NOT NULL DEFAULT NEWID(),
    UnitTypeId          int              NOT NULL,
    Code                varchar(50)      NOT NULL,
    Symbol              varchar(25)      NOT NULL,
    Decimals            int              NOT NULL DEFAULT (0),
    IsSystem            bit              NOT NULL DEFAULT (1),
    IsActive            bit              NOT NULL DEFAULT (1),
    CreatedUtc          datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy           varchar(150)         NULL,
    LastUpdatedUtc      datetime2            NULL,
    LastUpdatedBy       nvarchar(150)        NULL,

    CONSTRAINT PK_UnitsOfMeasure         PRIMARY KEY (UnitOfMeasureId),
    CONSTRAINT FK_UnitsOfMeasure_UnitTypes FOREIGN KEY (UnitTypeId) REFERENCES UnitTypes (UnitTypeId)
);
CREATE UNIQUE INDEX UQ_UnitsOfMeasure_Code ON UnitsOfMeasure (Code);
GO

-- ----------------------------------------------------------------
-- SubFamilies  (catalog — depends on Families)
-- ----------------------------------------------------------------
CREATE TABLE SubFamilies (
    SubFamilyId     int              NOT NULL IDENTITY(1,1),
    SubFamilyToken  uniqueidentifier NOT NULL DEFAULT NEWID(),
    FamilyId        int              NOT NULL,
    Code            varchar(100)     NOT NULL,
    IsSystem        bit              NOT NULL DEFAULT (1),
    IsActive        bit              NOT NULL DEFAULT (1),
    CreatedUtc      datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy       varchar(150)         NULL,
    LastUpdatedUtc  datetime2            NULL,
    LastUpdatedBy   nvarchar(150)        NULL,

    CONSTRAINT PK_SubFamilies         PRIMARY KEY (SubFamilyId),
    CONSTRAINT FK_SubFamilies_Families FOREIGN KEY (FamilyId) REFERENCES Families (FamilyId)
);
CREATE UNIQUE INDEX UX_SubFamilies ON SubFamilies (FamilyId, Code);
GO

-- ----------------------------------------------------------------
-- SubCategories  (catalog — depends on Categories)
-- ----------------------------------------------------------------
CREATE TABLE SubCategories (
    SubCategoryId     int              NOT NULL IDENTITY(1,1),
    SubCategoryToken  uniqueidentifier NOT NULL DEFAULT NEWID(),
    CategoryId        int              NOT NULL,
    Code              varchar(100)     NOT NULL,
    IsSystem          bit              NOT NULL DEFAULT (1),
    IsActive          bit              NOT NULL DEFAULT (1),
    CreatedUtc        datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy         varchar(150)         NULL,
    LastUpdatedUtc    datetime2            NULL,
    LastUpdatedBy     nvarchar(150)        NULL,

    CONSTRAINT PK_SubCategories           PRIMARY KEY (SubCategoryId),
    CONSTRAINT FK_SubCategories_Categories FOREIGN KEY (CategoryId) REFERENCES Categories (CategoryId)
);
CREATE UNIQUE INDEX UX_SubCategories ON SubCategories (CategoryId, Code);
GO

-- ----------------------------------------------------------------
-- UnitConversionRates  (catalog — depends on UnitsOfMeasure)
--   Only physical-unit conversions (same UnitType on both sides).
--   Commercial conversions are implicit in Article fields.
-- ----------------------------------------------------------------
CREATE TABLE UnitConversionRates (
    UnitConversionRateId     int              NOT NULL IDENTITY(1,1),
    UnitConversionRateToken  uniqueidentifier NOT NULL DEFAULT NEWID(),
    FromUnitOfMeasureId      int              NOT NULL,
    ToUnitOfMeasureId        int              NOT NULL,
    Factor                   decimal(18,8)    NOT NULL,
    IsActive                 bit              NOT NULL DEFAULT (1),
    CreatedUtc               datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy                varchar(150)         NULL,
    LastUpdatedUtc           datetime2            NULL,
    LastUpdatedBy            nvarchar(150)        NULL,

    CONSTRAINT PK_UnitConversionRates         PRIMARY KEY (UnitConversionRateId),
    CONSTRAINT FK_UnitConversionRates_FromUnit FOREIGN KEY (FromUnitOfMeasureId) REFERENCES UnitsOfMeasure (UnitOfMeasureId),
    CONSTRAINT FK_UnitConversionRates_ToUnit   FOREIGN KEY (ToUnitOfMeasureId)   REFERENCES UnitsOfMeasure (UnitOfMeasureId)
);
CREATE UNIQUE INDEX UQ_UnitConversionRates_From_To ON UnitConversionRates (FromUnitOfMeasureId, ToUnitOfMeasureId);
GO

-- ----------------------------------------------------------------
-- MenuItems  (self-referencing: ParentMenuItemId)
-- ----------------------------------------------------------------
CREATE TABLE MenuItems (
    MenuItemId       int              NOT NULL IDENTITY(1,1),
    MenuItemToken    uniqueidentifier NOT NULL DEFAULT NEWID(),
    ParentMenuItemId int                  NULL,
    Code             varchar(100)     NOT NULL,
    Title            varchar(150)     NOT NULL,
    Route            varchar(300)         NULL,
    Icon             varchar(100)         NULL,
    DisplayOrder     int              NOT NULL DEFAULT (0),
    IsActive         bit              NOT NULL DEFAULT (1),
    IsDeleted        bit              NOT NULL DEFAULT (0),
    CreatedUtc       datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy        varchar(150)         NULL,

    CONSTRAINT PK_MenuItems       PRIMARY KEY (MenuItemId),
    CONSTRAINT FK_MenuItems_Parent FOREIGN KEY (ParentMenuItemId) REFERENCES MenuItems (MenuItemId)
);
CREATE UNIQUE INDEX UQ_MenuItems_Code ON MenuItems (Code);
GO

-- ----------------------------------------------------------------
-- Users  (depends on Roles, Hotels, Suppliers)
-- ----------------------------------------------------------------
CREATE TABLE Users (
    UserId              int              NOT NULL IDENTITY(1,1),
    UserToken           uniqueidentifier NOT NULL DEFAULT NEWID(),
    FirstName           varchar(150)     NOT NULL,
    LastName            varchar(150)     NOT NULL,
    Email               varchar(320)     NOT NULL,
    NormalizedEmail     varchar(320)     NOT NULL,
    UserName            varchar(150)     NOT NULL,
    NormalizedUserName  varchar(150)     NOT NULL,
    PasswordHash        varchar(500)     NOT NULL,
    RoleId              int              NOT NULL,
    HotelId             int                  NULL,
    SupplierId          int                  NULL,
    IsActive            bit              NOT NULL DEFAULT (1),
    IsDeleted           bit              NOT NULL DEFAULT (0),
    EmailConfirmed      bit              NOT NULL DEFAULT (0),
    FailedLoginCount    int              NOT NULL DEFAULT (0),
    LastLoginUtc        datetime2            NULL,
    LockedUntilUtc      datetime2            NULL,
    CreatedUtc          datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy           varchar(150)         NULL,
    LastUpdatedUtc      datetime2            NULL,
    LastUpdatedBy       varchar(150)         NULL,
    DeletedUtc          datetime2            NULL,
    DeletedBy           varchar(150)         NULL,

    CONSTRAINT PK_Users        PRIMARY KEY (UserId),
    CONSTRAINT FK_Users_Roles     FOREIGN KEY (RoleId)     REFERENCES Roles     (RoleId),
    CONSTRAINT FK_Users_Hotels    FOREIGN KEY (HotelId)    REFERENCES Hotels    (HotelId),
    CONSTRAINT FK_Users_Suppliers FOREIGN KEY (SupplierId) REFERENCES Suppliers (SupplierId)
);
CREATE UNIQUE INDEX UQ_Users_UserToken                     ON Users (UserToken);
CREATE        INDEX IX_Users_RoleId                        ON Users (RoleId);
CREATE        INDEX IX_Users_HotelId                       ON Users (HotelId);
CREATE        INDEX IX_Users_SupplierId                    ON Users (SupplierId);
CREATE        INDEX UX_Users_NormalizedEmail_NotDeleted    ON Users (NormalizedEmail);
CREATE        INDEX UX_Users_NormalizedUserName_NotDeleted ON Users (NormalizedUserName);
CREATE        INDEX UX_Users_OneUserPerSupplier            ON Users (SupplierId);
GO

-- ----------------------------------------------------------------
-- RefreshTokens  (depends on Users)
-- ----------------------------------------------------------------
CREATE TABLE RefreshTokens (
    RefreshTokenId    int              NOT NULL IDENTITY(1,1),
    RefreshTokenToken uniqueidentifier NOT NULL DEFAULT NEWID(),
    UserId            int              NOT NULL,
    TokenHash         varchar(500)     NOT NULL,
    CreatedUtc        datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
    ExpiresUtc        datetime2        NOT NULL,
    IsRevoked         bit              NOT NULL DEFAULT (0),
    RevokedUtc        datetime2            NULL,
    CreatedByIp       varchar(100)         NULL,
    RevokedByIp       varchar(100)         NULL,
    UserAgent         varchar(500)         NULL,
    DeviceName        varchar(150)         NULL,
    ReplacedByToken   uniqueidentifier     NULL,
    SessionToken      uniqueidentifier     NULL,

    CONSTRAINT PK_RefreshTokens       PRIMARY KEY (RefreshTokenId),
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users (UserId)
);
CREATE UNIQUE INDEX UQ_RefreshTokens_Token   ON RefreshTokens (RefreshTokenToken);
CREATE        INDEX IX_RefreshTokens_UserId  ON RefreshTokens (UserId);
CREATE        INDEX IX_RefreshTokens_ExpiresUtc ON RefreshTokens (ExpiresUtc);
GO

-- ----------------------------------------------------------------
-- UserSessions  (depends on Users)
-- ----------------------------------------------------------------
CREATE TABLE UserSessions (
    UserSessionId int              NOT NULL IDENTITY(1,1),
    SessionToken  uniqueidentifier NOT NULL DEFAULT NEWID(),
    UserId        int              NOT NULL,
    LoginUtc      datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
    LogoutUtc     datetime2            NULL,
    IpAddress     varchar(100)         NULL,
    UserAgent     varchar(500)         NULL,
    DeviceName    varchar(150)         NULL,
    IsActive      bit              NOT NULL DEFAULT (1),

    CONSTRAINT PK_UserSessions       PRIMARY KEY (UserSessionId),
    CONSTRAINT FK_UserSessions_Users FOREIGN KEY (UserId) REFERENCES Users (UserId)
);
CREATE UNIQUE INDEX UQ_UserSessions_SessionToken ON UserSessions (SessionToken);
CREATE        INDEX IX_UserSessions_UserId        ON UserSessions (UserId);
GO

-- ----------------------------------------------------------------
-- ImpersonationSessions  (depends on Users)
-- ----------------------------------------------------------------
CREATE TABLE ImpersonationSessions (
    ImpersonationSessionId int              NOT NULL IDENTITY(1,1),
    ImpersonationToken     uniqueidentifier NOT NULL DEFAULT NEWID(),
    ActorUserId            int              NOT NULL,
    TargetUserId           int              NOT NULL,
    StartedUtc             datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
    EndedUtc               datetime2            NULL,
    Reason                 varchar(500)         NULL,
    IpAddress              varchar(100)         NULL,
    UserAgent              varchar(500)         NULL,

    CONSTRAINT PK_ImpersonationSessions        PRIMARY KEY (ImpersonationSessionId),
    CONSTRAINT FK_ImpersonationSessions_Actor  FOREIGN KEY (ActorUserId)  REFERENCES Users (UserId),
    CONSTRAINT FK_ImpersonationSessions_Target FOREIGN KEY (TargetUserId) REFERENCES Users (UserId)
);
CREATE UNIQUE INDEX UQ_ImpersonationSessions_Token ON ImpersonationSessions (ImpersonationToken);
GO

-- ----------------------------------------------------------------
-- AuditLogs  (depends on Users — FKs nullable for system actions)
-- ----------------------------------------------------------------
CREATE TABLE AuditLogs (
    AuditLogId         bigint           NOT NULL IDENTITY(1,1),
    TableName          varchar(150)     NOT NULL,
    RecordId           varchar(100)     NOT NULL,
    ActionType         varchar(50)      NOT NULL,
    OldValuesJson      nvarchar(MAX)        NULL,
    NewValuesJson      nvarchar(MAX)        NULL,
    ActorUserId        int                  NULL,
    EffectiveUserId    int                  NULL,
    ImpersonationToken uniqueidentifier     NULL,
    PerformedUtc       datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
    IpAddress          varchar(100)         NULL,
    UserAgent          varchar(500)         NULL,

    CONSTRAINT PK_AuditLogs              PRIMARY KEY (AuditLogId),
    CONSTRAINT FK_AuditLogs_ActorUser     FOREIGN KEY (ActorUserId)     REFERENCES Users (UserId),
    CONSTRAINT FK_AuditLogs_EffectiveUser FOREIGN KEY (EffectiveUserId) REFERENCES Users (UserId)
);
CREATE INDEX IX_AuditLogs_PerformedUtc ON AuditLogs (PerformedUtc);
CREATE INDEX IX_AuditLogs_Table_Record ON AuditLogs (TableName, RecordId);
GO

-- ----------------------------------------------------------------
-- HotelContacts  (depends on Hotels)
-- ----------------------------------------------------------------
CREATE TABLE HotelContacts (
    HotelContactId    int              NOT NULL IDENTITY(1,1),
    HotelContactToken uniqueidentifier NOT NULL DEFAULT NEWID(),
    HotelId           int              NOT NULL,
    ContactName       varchar(150)     NOT NULL,
    ContactType       varchar(100)         NULL,
    Department        varchar(100)         NULL,
    Phone             varchar(50)          NULL,
    Mobile            varchar(50)          NULL,
    Fax               varchar(50)          NULL,
    Email             varchar(320)         NULL,
    Notes             varchar(500)         NULL,
    IsPrimary         bit              NOT NULL DEFAULT (0),
    IsActive          bit              NOT NULL DEFAULT (1),
    IsDeleted         bit              NOT NULL DEFAULT (0),
    CreatedUtc        datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy         varchar(150)         NULL,
    LastUpdatedUtc    datetime2            NULL,
    LastUpdatedBy     varchar(150)         NULL,
    DeletedUtc        datetime2            NULL,
    DeletedBy         varchar(150)         NULL,

    CONSTRAINT PK_HotelContacts        PRIMARY KEY (HotelContactId),
    CONSTRAINT FK_HotelContacts_Hotels FOREIGN KEY (HotelId) REFERENCES Hotels (HotelId)
);
CREATE UNIQUE INDEX UQ_HotelContacts_Token ON HotelContacts (HotelContactToken);
GO

-- ----------------------------------------------------------------
-- HotelSuppliers  (depends on Hotels, Suppliers)
-- ----------------------------------------------------------------
CREATE TABLE HotelSuppliers (
    HotelSupplierId int          NOT NULL IDENTITY(1,1),
    HotelId         int          NOT NULL,
    SupplierId      int          NOT NULL,
    IsActive        bit          NOT NULL DEFAULT (1),
    CreatedUtc      datetime2    NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy       varchar(150)     NULL,
    LastUpdatedUtc  datetime2        NULL,
    LastUpdatedBy   varchar(150)     NULL,

    CONSTRAINT PK_HotelSuppliers         PRIMARY KEY (HotelSupplierId),
    CONSTRAINT FK_HotelSuppliers_Hotels    FOREIGN KEY (HotelId)    REFERENCES Hotels    (HotelId),
    CONSTRAINT FK_HotelSuppliers_Suppliers FOREIGN KEY (SupplierId) REFERENCES Suppliers (SupplierId)
);
CREATE UNIQUE INDEX UQ_HotelSuppliers_Hotel_Supplier ON HotelSuppliers (HotelId, SupplierId);
CREATE        INDEX IX_HotelSuppliers_HotelId        ON HotelSuppliers (HotelId);
CREATE        INDEX IX_HotelSuppliers_SupplierId     ON HotelSuppliers (SupplierId);
GO

-- ----------------------------------------------------------------
-- MenuAssignments  (depends on MenuItems, Roles)
-- ----------------------------------------------------------------
CREATE TABLE MenuAssignments (
    MenuAssignmentId int          NOT NULL IDENTITY(1,1),
    MenuItemId       int          NOT NULL,
    TargetType       varchar(50)  NOT NULL,
    TargetId         int              NULL,
    RoleId           int              NULL,
    IsEnabled        bit          NOT NULL DEFAULT (1),
    CreatedUtc       datetime2    NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy        varchar(150)     NULL,

    CONSTRAINT PK_MenuAssignments         PRIMARY KEY (MenuAssignmentId),
    CONSTRAINT FK_MenuAssignments_MenuItems FOREIGN KEY (MenuItemId) REFERENCES MenuItems (MenuItemId),
    CONSTRAINT FK_MenuAssignments_Roles     FOREIGN KEY (RoleId)     REFERENCES Roles     (RoleId)
);
CREATE INDEX IX_MenuAssignments_MenuItemId ON MenuAssignments (MenuItemId);
CREATE INDEX IX_MenuAssignments_RoleId     ON MenuAssignments (RoleId);
CREATE INDEX IX_MenuAssignments_Target     ON MenuAssignments (TargetType, TargetId);
GO

-- ----------------------------------------------------------------
-- Articles  (depends on Suppliers, UnitsOfMeasure, Families, SubFamilies)
--   Supplier-owned commercial SKU. One row = one sale format.
-- ----------------------------------------------------------------
CREATE TABLE Articles (
    ArticleId        int              NOT NULL IDENTITY(1,1),
    ArticleToken     uniqueidentifier NOT NULL DEFAULT NEWID(),
    SupplierId       int              NOT NULL,
    Name             varchar(250)     NOT NULL,
    NormalizedName   varchar(250)     NOT NULL,
    Description      varchar(1000)        NULL,
    SupplierSku      varchar(100)         NULL,
    Barcode          varchar(100)         NULL,
    Brand            varchar(150)         NULL,
    PurchaseUnitId   int              NOT NULL,    -- unit of the purchase package (BOX, PACK…)
    PurchaseQuantity decimal(18,6)    NOT NULL,    -- items inside the package (e.g. 24 bottles)
    ContentUnitId    int              NOT NULL,    -- physical content unit (MILLILITER, GRAM…)
    ContentQuantity  decimal(18,6)    NOT NULL,    -- content per item (e.g. 500 ml)
    FamilyId         int                  NULL,    -- supplier default classification
    SubFamilyId      int                  NULL,
    BaseUnitId       int                  NULL,    -- optional canonical physical unit for cross-unit reporting
    MinimumOrderQty  decimal(18,4)        NULL,    -- in purchase units
    LeadTimeDays     int                  NULL,
    IsActive         bit              NOT NULL DEFAULT (1),
    IsDeleted        bit              NOT NULL DEFAULT (0),
    CreatedUtc       datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy        varchar(150)         NULL,
    LastUpdatedUtc   datetime2            NULL,
    LastUpdatedBy    varchar(150)         NULL,
    DeletedUtc       datetime2            NULL,
    DeletedBy        varchar(150)         NULL,

    CONSTRAINT PK_Articles              PRIMARY KEY (ArticleId),
    CONSTRAINT FK_Articles_Suppliers    FOREIGN KEY (SupplierId)     REFERENCES Suppliers     (SupplierId),
    CONSTRAINT FK_Articles_PurchaseUnit FOREIGN KEY (PurchaseUnitId) REFERENCES UnitsOfMeasure (UnitOfMeasureId),
    CONSTRAINT FK_Articles_ContentUnit  FOREIGN KEY (ContentUnitId)  REFERENCES UnitsOfMeasure (UnitOfMeasureId),
    CONSTRAINT FK_Articles_FamilyId     FOREIGN KEY (FamilyId)       REFERENCES Families       (FamilyId),
    CONSTRAINT FK_Articles_SubFamilyId  FOREIGN KEY (SubFamilyId)    REFERENCES SubFamilies    (SubFamilyId),
    CONSTRAINT FK_Articles_BaseUnit     FOREIGN KEY (BaseUnitId)     REFERENCES UnitsOfMeasure (UnitOfMeasureId)
);
GO

-- ----------------------------------------------------------------
-- ArticlePrices  (depends on Articles, Hotels)
--   Insert-only: rows are never updated or deleted.
--   HotelId NULL  = global price for all hotels.
--   HotelId SET   = hotel-specific contract price (takes precedence).
-- ----------------------------------------------------------------
CREATE TABLE ArticlePrices (
    ArticlePriceId int           NOT NULL IDENTITY(1,1),
    ArticleId      int           NOT NULL,
    Price          decimal(18,6) NOT NULL,
    CreatedUtc     datetime2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy      varchar(150)      NULL,
    EffectiveDate  date          NOT NULL,    -- price valid from this date
    CurrencyCode   nvarchar(3)   NOT NULL,    -- ISO 4217
    HotelId        int               NULL,
    Notes          nvarchar(500)     NULL,

    CONSTRAINT PK_ArticlePrices          PRIMARY KEY (ArticlePriceId),
    CONSTRAINT FK_ArticlePrices_Articles FOREIGN KEY (ArticleId) REFERENCES Articles (ArticleId),
    CONSTRAINT FK_ArticlePrices_HotelId  FOREIGN KEY (HotelId)   REFERENCES Hotels   (HotelId)
);
GO

-- ----------------------------------------------------------------
-- HotelArticles  (depends on Hotels, Articles, Families, SubFamilies,
--                 Categories, SubCategories, UnitsOfMeasure)
--   Hotel-owned configuration of a supplier article.
--   Only created when a hotel actively engages with an article.
--   UNIQUE (HotelId, ArticleId) — one config row per combination.
-- ----------------------------------------------------------------
CREATE TABLE HotelArticles (
    HotelArticleId    int              NOT NULL IDENTITY(1,1),
    HotelArticleToken uniqueidentifier NOT NULL DEFAULT NEWID(),
    HotelId           int              NOT NULL,
    ArticleId         int              NOT NULL,
    IsActive          bit              NOT NULL DEFAULT (1),
    IsFavorite        bit              NOT NULL DEFAULT (0),
    InternalCode      varchar(100)         NULL,
    FamilyId          int                  NULL,    -- hotel override of supplier default
    SubFamilyId       int                  NULL,
    CategoryId        int                  NULL,
    SubCategoryId     int                  NULL,
    InventoryUnitId   int                  NULL,    -- NULL = not yet configured for inventory
    InventoryQuantity decimal(18,6)        NULL,    -- InventoryUnits per PurchaseUnit
    CreatedUtc        datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy         varchar(150)         NULL,
    LastUpdatedUtc    datetime2            NULL,
    LastUpdatedBy     varchar(150)         NULL,

    CONSTRAINT PK_HotelArticles               PRIMARY KEY (HotelArticleId),
    CONSTRAINT FK_HotelArticles_Hotels         FOREIGN KEY (HotelId)         REFERENCES Hotels        (HotelId),
    CONSTRAINT FK_HotelArticles_Articles       FOREIGN KEY (ArticleId)       REFERENCES Articles      (ArticleId),
    CONSTRAINT FK_HotelArticles_Families       FOREIGN KEY (FamilyId)        REFERENCES Families      (FamilyId),
    CONSTRAINT FK_HotelArticles_SubFamilies    FOREIGN KEY (SubFamilyId)     REFERENCES SubFamilies   (SubFamilyId),
    CONSTRAINT FK_HotelArticles_Categories     FOREIGN KEY (CategoryId)      REFERENCES Categories    (CategoryId),
    CONSTRAINT FK_HotelArticles_SubCategories  FOREIGN KEY (SubCategoryId)   REFERENCES SubCategories (SubCategoryId),
    CONSTRAINT FK_HotelArticles_InventoryUnit  FOREIGN KEY (InventoryUnitId) REFERENCES UnitsOfMeasure (UnitOfMeasureId)
);
CREATE UNIQUE INDEX UX_HotelArticles ON HotelArticles (HotelId, ArticleId);
GO

-- ================================================================
-- END OF SCHEMA SNAPSHOT
-- Stored procedures are in: database/stored-procedures/
-- ================================================================
