using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetInventoryTransferByTokenQueryRequest : IRequest<ApiResponse<GetInventoryTransferByTokenQueryResponse>>
    {
        public Guid InventoryTransferToken { get; set; }
    }
}
