using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetSuppliersQueryRequest : IRequest<ApiResponse<GetSuppliersQueryResponse>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchField { get; set; }
        public string? SearchText { get; set; }
        public bool IncludeInactive { get; set; } = false;

        // Narrows the zone delivery-coverage filter to this Warehouse's own Zone — omitted for
        // the general admin Suppliers catalog, which has no single warehouse in context.
        public Guid? WarehouseToken { get; set; }
    }
}
