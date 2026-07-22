namespace InnNou.Domain.Dtos
{
    public class OrderDto
    {
        public int OrderId { get; set; }
        public Guid OrderToken { get; set; }
        public int OrganizationId { get; set; }
        public Guid OrganizationToken { get; set; }
        public int WarehouseId { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public string Status { get; set; } = default!;
        public string? Notes { get; set; }
        public DateTime? SubmittedUtc { get; set; }

        // Relative URL to the order-confirmation PDF, populated best-effort by
        // OrderService.CompleteSubmissionAsync once PurchaseOrders are created — null until then,
        // and stays null if generation ever fails (never blocks order confirmation itself).
        // Downloaded via the authenticated POST /orders/downloadPdf endpoint, never served
        // statically (unlike Supplier.LogoUrl) since it carries prices/commercial data.
        public string? PdfUrl { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }

        // Always accurate regardless of code path: sp_Order_GetPaged computes it via a cheap
        // CROSS APPLY COUNT; every other path (GetByToken/Create/Submit/Cancel) sets it from
        // Lines.Count after hydrating Lines. Exists so list/summary views (which never hydrate
        // the full Lines collection, to avoid N+1) can still show an accurate line count.
        public int LineCount { get; set; }

        // Populated by OrderService after a second sp_OrderLine_GetByOrderId call — never
        // resolved by the mapper directly, same reasoning ArticleDto's IsFavorite required a
        // dedicated query rather than a lazy nav property.
        public List<OrderLineDto> Lines { get; set; } = [];

        // Same "second query, always populated" pattern as Lines above — includes terminal
        // (REJECTED/CANCELLED) rows from a past attempt too, so the detail view shows the full
        // approval history, not just what's currently pending.
        public List<OrderApprovalStepDto> ApprovalSteps { get; set; } = [];
    }
}
