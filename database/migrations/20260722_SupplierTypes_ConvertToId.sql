-- Converts Suppliers.SupplierType from a CHECK-constrained varchar to an Id-backed
-- lookup table (SupplierTypes). See 20260722_OrderStatuses_ConvertToId.sql for the
-- full rationale — same recipe applied here.
--
-- Suppliers has filtered unique indexes (NormalizedName scoping — see CLAUDE.md's
-- Supplier global/private scoping note), so any data-modifying statement against it
-- needs these SET options explicitly, same "Gotcha" as every SP writing to this table.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.SupplierTypes', 'U') IS NULL
BEGIN
    CREATE TABLE SupplierTypes (
        SupplierTypeId int         NOT NULL IDENTITY(1,1),
        Code           varchar(20) NOT NULL,
        IsActive       bit         NOT NULL DEFAULT 1,

        CONSTRAINT PK_SupplierTypes PRIMARY KEY (SupplierTypeId),
        CONSTRAINT UQ_SupplierTypes_Code UNIQUE (Code)
    );
END
GO

-- Seed order matters — the C# SupplierType enum hardcodes these Ids (Product=1, Service=2, Mixed=3).
IF NOT EXISTS (SELECT 1 FROM SupplierTypes WHERE Code = 'PRODUCT')
    INSERT INTO SupplierTypes (Code) VALUES ('PRODUCT');
GO
IF NOT EXISTS (SELECT 1 FROM SupplierTypes WHERE Code = 'SERVICE')
    INSERT INTO SupplierTypes (Code) VALUES ('SERVICE');
GO
IF NOT EXISTS (SELECT 1 FROM SupplierTypes WHERE Code = 'MIXED')
    INSERT INTO SupplierTypes (Code) VALUES ('MIXED');
GO

IF COL_LENGTH('dbo.Suppliers', 'SupplierTypeId') IS NULL
    ALTER TABLE Suppliers ADD SupplierTypeId INT NULL;
GO

UPDATE s
    SET s.SupplierTypeId = st.SupplierTypeId
FROM Suppliers s
JOIN SupplierTypes st ON st.Code = s.SupplierType
WHERE s.SupplierTypeId IS NULL;
GO

IF EXISTS (SELECT 1 FROM Suppliers WHERE SupplierTypeId IS NULL)
    THROW 51003, 'SupplierTypeId backfill incomplete — some Supplier rows have an unrecognized SupplierType value.', 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id WHERE t.name = 'Suppliers' AND c.name = 'SupplierTypeId' AND c.is_nullable = 0)
    ALTER TABLE Suppliers ALTER COLUMN SupplierTypeId INT NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Suppliers_SupplierTypeId')
    ALTER TABLE Suppliers ADD CONSTRAINT DF_Suppliers_SupplierTypeId DEFAULT (1) FOR SupplierTypeId; -- 1 = PRODUCT
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Suppliers_SupplierTypes')
    ALTER TABLE Suppliers ADD CONSTRAINT FK_Suppliers_SupplierTypes FOREIGN KEY (SupplierTypeId) REFERENCES SupplierTypes (SupplierTypeId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Suppliers_SupplierTypeId')
    CREATE INDEX IX_Suppliers_SupplierTypeId ON Suppliers (SupplierTypeId);
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Suppliers_SupplierType')
    ALTER TABLE Suppliers DROP CONSTRAINT CK_Suppliers_SupplierType;
GO

DECLARE @dfName sysname;
SELECT @dfName = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.Suppliers') AND c.name = 'SupplierType';
IF @dfName IS NOT NULL
    EXEC('ALTER TABLE Suppliers DROP CONSTRAINT ' + @dfName);
GO

IF COL_LENGTH('dbo.Suppliers', 'SupplierType') IS NOT NULL
    ALTER TABLE Suppliers DROP COLUMN SupplierType;
GO
