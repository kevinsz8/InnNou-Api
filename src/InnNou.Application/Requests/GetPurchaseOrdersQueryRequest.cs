using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetPurchaseOrdersQueryRequest : IRequest<ApiResponse<GetPurchaseOrdersQueryResponse>>
    {
        public Guid? OrganizationToken { get; set; }
        public Guid? OrderToken { get; set; }
        public string? Status { get; set; }

        // Multi-value alternative to Status (same STRING_SPLIT convention as GetUsers'
        // RoleIds/OrganizationIds) — lets a caller fetch e.g. every still-receivable PurchaseOrder
        // (SENT + PARTIALLY_RECEIVED) in one paginated call instead of merging two separate pages
        // client-side. Purely an additional narrowing filter; if both Status and Statuses are set,
        // both apply (AND).
        public List<string>? Statuses { get; set; }

        // Case-insensitive partial match against PurchaseOrderNumber — added for the "Recepciones"
        // page's Pendientes de recepcion tab, which needs a real server-side-paginated, searchable
        // list of receivable POs rather than a single bounded client-filtered fetch.
        public string? PurchaseOrderNumber { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
