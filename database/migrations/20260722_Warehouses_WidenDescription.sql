-- Widens Warehouses.Description from VARCHAR(500) to VARCHAR(MAX) — needed for a future
-- order-email/PDF use (not wired up yet). Idempotent/rerunnable.

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS (
    SELECT 1 FROM sys.columns c
    JOIN sys.types t ON t.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID('dbo.Warehouses') AND c.name = 'Description' AND t.name != 'text' AND c.max_length != -1
)
    ALTER TABLE Warehouses ALTER COLUMN Description VARCHAR(MAX) NULL;
GO
