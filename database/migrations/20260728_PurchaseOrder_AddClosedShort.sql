/* =============================================================
   PURCHASE ORDER - ADD CLOSED_SHORT STATUS ("Caso B")
   Closes a real gap Purchase Order Rectifications ("Caso A") didn't
   cover: a PARTIALLY_RECEIVED PurchaseOrder where the supplier simply
   never sends the rest and the buyer stops chasing it, with no mutual
   agreement to formally reduce the order (that's Rectify's job).

   Deliberately does NOT touch PurchaseOrderLine/Quantity — the point
   is to preserve the shortfall as a real, permanent fact (needed for
   the future Supplier Scorecard's OTIF/rejection-rate KPIs), not to
   rewrite history the way a Rectification would. Symmetric to CANCELLED
   (SENT, nothing arrived, give up) but for "something arrived, not all
   of it, and we're done waiting" from PARTIALLY_RECEIVED.

   5th PurchaseOrderStatuses value — appended, never renumbering
   SENT=1/CANCELLED=2/PARTIALLY_RECEIVED=3/RECEIVED=4, same convention
   as 20260726_PurchaseOrderStatuses_AddReceivingStatuses.sql.
   ============================================================= */

INSERT INTO dbo.PurchaseOrderStatuses (Code, IsActive)
VALUES ('CLOSED_SHORT', 1);

ALTER TABLE dbo.PurchaseOrder ADD
    ClosedShortUtc    DATETIME2     NULL,
    ClosedShortBy     VARCHAR(150)  NULL,
    ClosedShortReason NVARCHAR(500) NULL;
