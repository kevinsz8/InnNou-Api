using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateInternalOrderLineRequestItem
    {
        public Guid ArticleToken { get; set; }
        public decimal Quantity { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateInternalOrderCommandRequest : IRequest<ApiResponse<CreateInternalOrderCommandResponse>>
    {
        public Guid SourceOrganizationToken { get; set; }
        public Guid DestinationWarehouseToken { get; set; }
        public string? Notes { get; set; }
        public List<CreateInternalOrderLineRequestItem> Lines { get; set; } = [];
    }
}
