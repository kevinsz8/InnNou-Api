using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetGoodsReceiptsPagedQueryRequest : IRequest<ApiResponse<GetGoodsReceiptsPagedQueryResponse>>
    {
        public Guid? OrganizationToken { get; set; }
        public Guid? WarehouseToken { get; set; }
        public string? PurchaseOrderNumber { get; set; }
        public string? DeliveryNoteNumber { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
