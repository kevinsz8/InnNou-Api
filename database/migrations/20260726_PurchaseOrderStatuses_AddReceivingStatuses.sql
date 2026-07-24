-- Adds PARTIALLY_RECEIVED (Id 3) and RECEIVED (Id 4) to PurchaseOrderStatuses for the new
-- Goods Receipts module. Appends only — never renumber SENT (1) / CANCELLED (2), the C#
-- PurchaseOrderStatus enum hardcodes these Ids (see PurchaseOrderStatus.cs).

IF NOT EXISTS (SELECT 1 FROM PurchaseOrderStatuses WHERE Code = 'PARTIALLY_RECEIVED')
    INSERT INTO PurchaseOrderStatuses (Code) VALUES ('PARTIALLY_RECEIVED');
GO
IF NOT EXISTS (SELECT 1 FROM PurchaseOrderStatuses WHERE Code = 'RECEIVED')
    INSERT INTO PurchaseOrderStatuses (Code) VALUES ('RECEIVED');
GO
