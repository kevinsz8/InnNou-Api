using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetWarehouseContactByTokenQueryRequest : IRequest<ApiResponse<GetWarehouseContactByTokenQueryResponse>>
    {
        public Guid WarehouseContactToken { get; set; }
    }
}
